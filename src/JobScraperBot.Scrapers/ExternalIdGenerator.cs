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
            return officialId;

        if (!string.IsNullOrWhiteSpace(url))
        {
            var normalized = NormalizeUrl(url);

            if (Uri.TryCreate(normalized, UriKind.Absolute, out var uri))
            {
                var last = uri.Segments.LastOrDefault()?.Trim('/');
                if (!string.IsNullOrWhiteSpace(last))
                    return last;
            }

            return HashString(normalized);
        }

        var composite = string.Join("|",
            site?.Trim() ?? "",
            title?.Trim() ?? "",
            company?.Trim() ?? "",
            location?.Trim() ?? "");

        return HashString(composite.ToLowerInvariant());
    }

    private static string NormalizeUrl(string url)
    {
        var value = url.Trim();

        if (!value.StartsWith("http://", StringComparison.OrdinalIgnoreCase) &&
            !value.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
        {
            value = "https://" + value;
        }

        if (Uri.TryCreate(value, UriKind.Absolute, out var uri))
        {
            var builder = new UriBuilder(uri)
            {
                Query = string.Empty,
                Fragment = string.Empty
            };

            return builder.Uri.GetLeftPart(UriPartial.Path).TrimEnd('/');
        }

        return value;
    }

    private static string HashString(string input)
    {
        var bytes = Encoding.UTF8.GetBytes(input);
        var hash = SHA256.HashData(bytes);
        return Convert.ToHexString(hash.AsSpan(0, 12)).ToLowerInvariant();
    }
}
