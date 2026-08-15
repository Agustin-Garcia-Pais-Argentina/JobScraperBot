using System.Net;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;

namespace JobScraperBot.Scrapers.Utn;

/// <summary>Una convocatoria de pasantía detectada en el HTML, antes de bajar el PDF.</summary>
public record PasantiaConvocatoria(string ReferenceCode, string PdfUrl, IReadOnlyList<string> CareerCodes);

/// <summary>
/// Extrae, de la página de Pasantías de UTN, cada convocatoria abierta
/// (link "CONVOCATORIA REF [...]" -> PDF) junto con los códigos de carrera
/// (íconos) que la preceden -- eso es lo que se usa como filtro de carrera,
/// sin necesidad de abrir el PDF.
///
/// A propósito NO navega el árbol DOM (la jerarquía de <div> de esta página
/// es inconsistente entre módulos de Joomla, confirmado con DevTools). En
/// cambio, todo se resuelve por ORDEN TEXTUAL en el HTML crudo vía regex:
/// a cada convocatoria se le asignan los <img alt="..."> que aparecen entre
/// el final de la convocatoria anterior y el inicio de esta. Ese orden es
/// estable aunque la anidación de <div> no lo sea.
/// </summary>
public static class UtnPasantiasHtmlMapper
{
    public const string SiteName = "UTN Santa Fe - Pasantías";
    private const string SiteOrigin = "https://www.frsf.utn.edu.ar";

    // \s y &nbsp; cubiertos los dos -- el sitio mezcla espacio normal y no
    // separable sin previo aviso (ya lo vimos en Búsquedas Laborales).
    private static readonly Regex SectionHeadingRegex = new(
        @"CONVOCATORIAS(?:\s|&nbsp;)+ABIERTAS", RegexOptions.IgnoreCase | RegexOptions.Compiled);

    private static readonly Regex ConvocatoriaRegex = new(
        @"CONVOCATORIA(?:\s|&nbsp;)+REF[\s\S]{0,200}?<a\b[^>]*\bhref\s*=\s*[""']([^""']+)[""'][^>]*>\s*([^<]+?)\s*</a>",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    private static readonly Regex ImgAltRegex = new(
        @"<img\b[^>]*\balt\s*=\s*[""']([^""']*)[""'][^>]*>",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    public static Task<IReadOnlyList<PasantiaConvocatoria>> ExtractConvocatoriasAsync(string html, ILogger? logger = null)
    {
        var headingMatch = SectionHeadingRegex.Match(html);

        if (!headingMatch.Success)
        {
            logger?.LogWarning(
                "[{Site}] No se encontró el texto \"CONVOCATORIAS ABIERTAS\" en el HTML -- el sitio puede haber cambiado su contenido",
                SiteName);
            return Task.FromResult<IReadOnlyList<PasantiaConvocatoria>>(Array.Empty<PasantiaConvocatoria>());
        }

        var searchSpace = html[(headingMatch.Index + headingMatch.Length)..];

        var convocatoriaMatches = ConvocatoriaRegex.Matches(searchSpace).Cast<Match>().ToList();
        var imgMatches = ImgAltRegex.Matches(searchSpace).Cast<Match>().ToList();

        if (convocatoriaMatches.Count == 0)
        {
            logger?.LogWarning(
                "[{Site}] Se encontró el encabezado pero no se extrajo ninguna convocatoria -- puede que no haya ninguna abierta ahora, o que el formato del link haya cambiado",
                SiteName);
            return Task.FromResult<IReadOnlyList<PasantiaConvocatoria>>(Array.Empty<PasantiaConvocatoria>());
        }

        var result = new List<PasantiaConvocatoria>();
        var previousEnd = 0;

        foreach (var match in convocatoriaMatches)
        {
            // Íconos de carrera que aparecen ENTRE la convocatoria anterior y esta.
            var careerCodes = imgMatches
                .Where(im => im.Index >= previousEnd && im.Index < match.Index)
                .Select(im => im.Groups[1].Value.Trim())
                .Where(c => c.Length > 0)
                .ToList();

            var referenceCode = WebUtility.HtmlDecode(match.Groups[2].Value.Trim());
            var pdfUrl = ResolveUrl(WebUtility.HtmlDecode(match.Groups[1].Value.Trim()));

            result.Add(new PasantiaConvocatoria(referenceCode, pdfUrl, careerCodes));
            previousEnd = match.Index + match.Length;
        }

        var deduped = result.GroupBy(c => c.PdfUrl).Select(g => g.First()).ToList();
        return Task.FromResult<IReadOnlyList<PasantiaConvocatoria>>(deduped);
    }

    private static string ResolveUrl(string href)
        => href.StartsWith("http", StringComparison.OrdinalIgnoreCase) ? href : SiteOrigin + href;
}