using System.Text.Json;
using System.Text.Json.Serialization;
using JobScraperBot.Core.Models;
using Microsoft.Extensions.Logging;

namespace JobScraperBot.Scrapers;

/// <summary>
/// Parseo del JSON de RemoteOK separado de la llamada HTTP, para poder
/// testearlo contra un fixture guardado sin pegarle a la API real.
/// </summary>
public static class RemoteOkMapper
{
    public const string SiteName = "RemoteOK";

    public static IReadOnlyList<JobOffer> MapJson(string json, ILogger? logger = null)
    {
        List<RemoteOkJob>? jobs;
        try
        {
            jobs = JsonSerializer.Deserialize<List<RemoteOkJob>>(json);
        }
        catch (JsonException ex)
        {
            logger?.LogWarning(ex, "[{Site}] No se pudo parsear la respuesta JSON -- posible cambio de contrato de la API", SiteName);
            return Array.Empty<JobOffer>();
        }

        if (jobs is null || jobs.Count <= 1)
        {
            logger?.LogWarning("[{Site}] La respuesta no trajo ofertas.", SiteName);
            return Array.Empty<JobOffer>();
        }

        // El índice 0 es el legal disclaimer de RemoteOK, no una oferta real.
        return jobs.Skip(1).Select(MapToJobOffer).ToList();
    }

    private static JobOffer MapToJobOffer(RemoteOkJob job) => new(
        SourceSite: SiteName,
        ExternalId: job.Id,
        Title: job.Position ?? "",
        Company: job.Company ?? "",
        Location: job.Location ?? "Global",
        IsRemote: true,
        Url: job.Url ?? "",
        PublishedAt: job.Date,
        Salary: null,
        RawDescription: job.Description ?? ""
    );

    internal record RemoteOkJob(
        [property: JsonPropertyName("id")] string Id,
        [property: JsonPropertyName("url")] string Url,
        [property: JsonPropertyName("position")] string Position,
        [property: JsonPropertyName("company")] string Company,
        [property: JsonPropertyName("location")] string Location,
        [property: JsonPropertyName("date")] DateTime Date,
        [property: JsonPropertyName("description")] string Description
    );
}