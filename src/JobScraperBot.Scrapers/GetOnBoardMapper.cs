using System.Text.Json;
using System.Text.Json.Serialization;
using JobScraperBot.Core.Models;
using Microsoft.Extensions.Logging;

namespace JobScraperBot.Scrapers;

/// <summary>
/// Parseo del JSON de Get on Board separado de la llamada HTTP, para poder
/// testearlo contra un fixture guardado sin pegarle a la API real.
/// </summary>
public static class GetOnBoardMapper
{
    public const string SiteName = "GetOnBoard";

    public static IReadOnlyList<JobOffer> MapJson(string json, ILogger? logger = null)
    {
        GetOnBoardResponse? response;
        try
        {
            response = JsonSerializer.Deserialize<GetOnBoardResponse>(json);
        }
        catch (JsonException ex)
        {
            logger?.LogWarning(ex, "[{Site}] No se pudo parsear la respuesta JSON -- posible cambio de contrato de la API", SiteName);
            return Array.Empty<JobOffer>();
        }

        if (response?.Data is null || response.Data.Count == 0)
        {
            logger?.LogWarning("[{Site}] La respuesta no trajo ofertas o cambió el contrato de la API.", SiteName);
            return Array.Empty<JobOffer>();
        }

        return response.Data.Select(MapToJobOffer).ToList();
    }

    private static JobOffer MapToJobOffer(GetOnBoardJob job)
    {
        var attributes = job.Attributes;
        var companyName = attributes?.Company?.Data?.Attributes?.Name ?? "Empresa Confidencial";
        var publishedDate = DateTimeOffset.FromUnixTimeSeconds(attributes?.PublishedAt ?? 0).UtcDateTime;
        var jobUrl = job.Links?.PublicUrl ?? $"https://www.getonbrd.com/jobs/{job.Id}";

        return new JobOffer(
            SourceSite: SiteName,
            ExternalId: job.Id ?? Guid.NewGuid().ToString(),
            Title: attributes?.Title ?? "Sin título",
            Company: companyName,
            Location: attributes?.Remote == true ? "Remote" : "On-site",
            IsRemote: attributes?.Remote ?? false,
            Url: jobUrl,
            PublishedAt: publishedDate,
            Salary: null,
            RawDescription: attributes?.Description ?? ""
        );
    }

    internal record GetOnBoardResponse([property: JsonPropertyName("data")] List<GetOnBoardJob> Data);

    internal record GetOnBoardJob(
        [property: JsonPropertyName("id")] string Id,
        [property: JsonPropertyName("attributes")] GetOnBoardAttributes? Attributes,
        [property: JsonPropertyName("links")] GetOnBoardLinks? Links
    );

    internal record GetOnBoardLinks([property: JsonPropertyName("public_url")] string? PublicUrl);

    internal record GetOnBoardAttributes(
        [property: JsonPropertyName("title")] string Title,
        [property: JsonPropertyName("description")] string Description,
        [property: JsonPropertyName("remote")] bool Remote,
        [property: JsonPropertyName("published_at")] long PublishedAt,
        [property: JsonPropertyName("company")] GetOnBoardCompanyWrapper? Company
    );

    internal record GetOnBoardCompanyWrapper([property: JsonPropertyName("data")] GetOnBoardCompanyData? Data);
    internal record GetOnBoardCompanyData([property: JsonPropertyName("attributes")] GetOnBoardCompanyAttributes? Attributes);
    internal record GetOnBoardCompanyAttributes([property: JsonPropertyName("name")] string Name);
}