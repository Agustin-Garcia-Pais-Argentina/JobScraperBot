using System.Net.Http.Json;
using System.Text.Json.Serialization;
using JobScraperBot.Core.Interfaces;
using JobScraperBot.Core.Models;
using Microsoft.Extensions.Logging;

namespace JobScraperBot.Scrapers;

public class RemoteOkScraper : IJobScraper
{
    private readonly HttpClient _http;
    private readonly ILogger<RemoteOkScraper> _logger;

    public string SiteName => "RemoteOK";

    public RemoteOkScraper(HttpClient http, ILogger<RemoteOkScraper> logger)
    {
        _http = http;
        _logger = logger;
    }

    public async Task<IReadOnlyList<JobOffer>> ScrapeAsync(CancellationToken ct)
    {
        // RemoteOK suele bloquear peticiones sin User-Agent
        if (!_http.DefaultRequestHeaders.Contains("User-Agent"))
        {
            _http.DefaultRequestHeaders.TryAddWithoutValidation("User-Agent", "JobScraperBot/1.0");
        }

        // Le pegamos al endpoint general filtrando por 'dev'
        var response = await _http.GetFromJsonAsync<List<RemoteOkJob>>(
            "https://remoteok.com/api?tag=dev", cancellationToken: ct);

        if (response is null || response.Count <= 1)
        {
            _logger.LogWarning("[{Site}] La respuesta no trajo ofertas.", SiteName);
            return Array.Empty<JobOffer>();
        }

        // Ignoramos el índice 0 que es el legal disclaimer de RemoteOK
        return response.Skip(1).Select(MapToJobOffer).ToList();
    }

    private JobOffer MapToJobOffer(RemoteOkJob job) => new(
        SourceSite: SiteName,
        ExternalId: job.Id,
        Title: job.Position ?? "",
        Company: job.Company ?? "",
        Location: job.Location ?? "Global",
        IsRemote: true, 
        Url: job.Url ?? "",
        PublishedAt: job.Date,
        Salary: null, // RemoteOK lo manda numérico min/max, lo omitimos para simplificar
        RawDescription: job.Description ?? ""
    );

    // Mapeamos los campos exactos del JSON de RemoteOK
    private record RemoteOkJob(
        [property: JsonPropertyName("id")] string Id,
        [property: JsonPropertyName("url")] string Url,
        [property: JsonPropertyName("position")] string Position,
        [property: JsonPropertyName("company")] string Company,
        [property: JsonPropertyName("location")] string Location,
        [property: JsonPropertyName("date")] DateTime Date,
        [property: JsonPropertyName("description")] string Description
    );
}