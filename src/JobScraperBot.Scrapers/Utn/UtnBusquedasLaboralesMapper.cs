using System.Text.RegularExpressions;
using AngleSharp;
using AngleSharp.Dom;
using JobScraperBot.Core.Models;
using Microsoft.Extensions.Logging;

namespace JobScraperBot.Scrapers.Utn;

/// <summary>
/// Parseo de "Búsquedas Laborales" de UTN Santa Fe. Cada búsqueda es un
/// bloque de texto corrido dentro de .item-page / .com-content-article__body,
/// separado por el encabezado "Búsqueda Laboral número de referencia: NNNNN".
/// Parseo por texto + regex sobre los <h4>/<p> del cuerpo (a cualquier
/// profundidad, no solo hijos directos), en vez de por selectores CSS por tarjeta.
/// </summary>
public static class UtnBusquedasLaboralesMapper
{
    public const string SiteName = "UTN Santa Fe - Búsquedas Laborales";
    private const string ListingUrl = "https://www.frsf.utn.edu.ar/graduados/busquedas-laborales";
    private const string SiteOrigin = "https://www.frsf.utn.edu.ar";

    private static readonly Regex ReferenceSplitter = new(
        @"Búsqueda Laboral número de referencia:\s*(\d+)", RegexOptions.Compiled);

    private static readonly Regex NombrePrestacionRegex = new(@"Nombre de la prestación:\s*(.+)", RegexOptions.Compiled);
    private static readonly Regex EmpresaRegex = new(@"Empresa/Institución:\s*(.+)", RegexOptions.Compiled);
    private static readonly Regex DestinadaARegex = new(@"Destinada a:\s*(.+)", RegexOptions.Compiled);
    private static readonly Regex CarreraRegex = new(@"Carrera:\s*(.+)", RegexOptions.Compiled);

    // CV.{0,2}s cubre "CV's", "CV´s", "CVs" y variantes -- no confiar en un
    // único carácter de apóstrofe, ya vimos que el sitio usa entidades raras.
    private static readonly Regex PlazoRegex = new(
        @"Plazo m[aá]ximo de recepci[oó]n de CV.{0,2}s:\s*(.+)", RegexOptions.Compiled);

    public static async Task<IReadOnlyList<JobOffer>> MapListingHtmlAsync(string html, ILogger? logger = null)
    {
        using var context = BrowsingContext.New(Configuration.Default);
        var document = await context.OpenAsync(req => req.Content(html));

        var articleBody = document.QuerySelector(".com-content-article__body")
            ?? document.QuerySelector("[itemprop='articleBody']")
            ?? document.QuerySelector(".item-page");

        if (articleBody is null)
        {
            logger?.LogWarning(
                "[{Site}] No se encontró el cuerpo del artículo -- el sitio puede haber cambiado su plantilla",
                SiteName);
            return Array.Empty<JobOffer>();
        }

        return ParseListings(articleBody, logger);
    }

    private static List<JobOffer> ParseListings(IElement articleBody, ILogger? logger)
    {
        var offers = new List<JobOffer>();

        // OJO: usar QuerySelectorAll("h4, p") en vez de .Children -- el contenido
        // real está anidado un nivel más adentro que el contenedor que matcheamos
        // (.item-page > .com-content-article__body > h4/p...), así que solo mirar
        // hijos directos no alcanza.
        var textBlocks = articleBody.QuerySelectorAll("h4, p")
            .Select(e => e.TextContent.Trim())
            .Where(t => t.Length > 0)
            .ToList();

        // El sitio usa &nbsp; (espacio no separable, U+00A0) en vez de espacio
        // normal en algunos textos (ej. "Búsqueda Laboral<nbsp>número de..."),
        // lo cual rompe los regex si no se normaliza antes de matchear.
        var fullText = string.Join("\n", textBlocks).Replace('\u00A0', ' ');

        var matches = ReferenceSplitter.Matches(fullText);

        if (matches.Count == 0)
        {
            logger?.LogWarning(
                "[{Site}] No se encontraron búsquedas laborales -- puede que no haya publicaciones activas ahora, o que el formato del texto haya cambiado. Primeros textos vistos: {Preview}",
                SiteName, string.Join(" | ", textBlocks.Take(5)));
            return offers;
        }

        var pdfLinks = articleBody.QuerySelectorAll("a")
            .Select(a => a.GetAttribute("href") ?? "")
            .Where(href => href.Contains("/images/", StringComparison.OrdinalIgnoreCase))
            .Select(ResolveUrl)
            .ToList();

        for (int i = 0; i < matches.Count; i++)
        {
            var referenceId = matches[i].Groups[1].Value;
            var start = matches[i].Index;
            var end = i + 1 < matches.Count ? matches[i + 1].Index : fullText.Length;
            var block = fullText[start..end];

            var title = NombrePrestacionRegex.Match(block).Groups[1].Value.Trim();
            var company = EmpresaRegex.Match(block).Groups[1].Value.Trim();
            var audience = DestinadaARegex.Match(block).Groups[1].Value.Trim();
            var career = CarreraRegex.Match(block).Groups[1].Value.Trim();
            var deadline = PlazoRegex.Match(block).Groups[1].Value.Trim();
            var pdfUrl = i < pdfLinks.Count ? pdfLinks[i] : "";

            if (string.IsNullOrWhiteSpace(title))
            {
                logger?.LogWarning(
                    "[{Site}] No se pudo extraer el título de la búsqueda #{Ref}, se omite", SiteName, referenceId);
                continue;
            }

            var description = $"{title}. Destinada a: {audience}. Carrera: {career}. Plazo de recepción de CVs: {deadline}.";

            offers.Add(new JobOffer(
                SourceSite: SiteName,
                ExternalId: referenceId,
                Title: title,
                Company: company,
                Location: "",
                IsRemote: false,
                Url: string.IsNullOrEmpty(pdfUrl) ? ListingUrl : pdfUrl,
                PublishedAt: DateTime.UtcNow,
                Salary: null,
                RawDescription: description
            ));
        }

        return offers;
    }

    private static string ResolveUrl(string href)
        => href.StartsWith("http", StringComparison.OrdinalIgnoreCase) ? href : SiteOrigin + href;
}