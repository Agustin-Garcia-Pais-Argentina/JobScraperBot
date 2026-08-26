using System;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace JobScraperBot.Scrapers.Helpers;

/// <summary>
/// Genera ExternalId deterministas para las ofertas siguiendo la prioridad:
/// 1) ID oficial del sitio (si está disponible)
/// 2) URL absoluta y normalizada
/// 3) Hash estable de site + título + empresa + ubicación
/// 
/// Este helper está separado para poder testearlo fácilmente.
/// </summary>
public static class ExternalIdGenerator
{
    public static string Generate(
        string site,
        string? officialId,
        string? url,
        string? title,
        string? company,
        string? location)
    {
        if (!string.IsNullOrWhiteSpace(officialId))
            return officialId!;

        if (!string.IsNullOrWhiteSpace(url))
        {
            try
            {
                var normalized = NormalizeUrl(url!);
                // Si la url termina en un segmento no vacío, usar ese slug (por legibilidad).
                var last = new Uri(normalized).Segments.LastOrDefault()?.Trim('/');
                if (!string.IsNullOrWhiteSpace(last))
                    return last!;

                // Si no hay segmento útil, usar el hash de la URL para mantener estabilidad.
                return HashString(normalized);
            }
            catch
            {
                // Si la normalización falla por alguna razón, caemos al hash compuesto.
            }
        }

        // Fallback: hash estable de los campos más confiables disponibles
        var composite = string.Join("|", new[] { site ?? "", title ?? "", company ?? "", location ?? "" });
        return HashString(composite);
    }

    private static string NormalizeUrl(string url)
    {
        // Asegurar esquema y quitar query/fragment, y terminar sin slash
        if (!url.StartsWith("http://", StringComparison.OrdinalIgnoreCase) &&
            !url.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
        {
            // asumir https si falta
            url = "https://" + url;
        }

        var uri = new Uri(url);
        var builder = new UriBuilder(uri)
        {
            Query = string.Empty,
            Fragment = string.Empty
        };
        var result = builder.Uri.GetLeftPart(UriPartial.Path).TrimEnd('/');
        return result;
    }

    private static string HashString(string input)
    {
        using var sha256 = SHA256.Create();
        var bytes = Encoding.UTF8.GetBytes(input);
        var hash = sha256.ComputeHash(bytes);
        // hex short: tomar 12 bytes (24 hex chars) para mantenerlo legible y suficientemente único
        return string.Concat(hash.Take(12).Select(b => b.ToString("x2")));
    }
}
