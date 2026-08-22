using System.Net.Http.Json;
using System.Text.Json.Serialization;
using JobScraperBot.Core.Interfaces;
using JobScraperBot.Core.Models;
using Microsoft.Extensions.Logging;

namespace JobScraperBot.Scrapers;

public class GetOnBoardScraper : IJobScraper
{
    private readonly HttpClient _http;
    private readonly ILogger<GetOnBoardScraper> _logger;

    public string SiteName => "GetOnBoard";

    public GetOnBoardScraper(HttpClient http, ILogger<GetOnBoardScraper> logger)
    {
        _http = http;
        _logger = logger;
    }

    public async Task<IReadOnlyList<JobOffer>> ScrapeAsync(CancellationToken ct)
    {
        var url = "https://www.getonbrd.com/api/v0/categories/programming/jobs?per_page=50&expand=[\"company\"]";
        
        var response = await _http.GetFromJsonAsync<GetOnBoardResponse>(url, cancellationToken: ct);

        if (response?.Data is null || !response.Data.Any())
        {
            _logger.LogWarning("[{Site}] La respuesta no trajo ofertas o cambió el contrato de la API.", SiteName);
            return Array.Empty<JobOffer>();
        }

        return response.Data.Select(MapToJobOffer).ToList();
    }

    private JobOffer MapToJobOffer(GetOnBoardJob job)
    {
        var attributes = job.Attributes;
        var companyName = attributes?.Company?.Data?.Attributes?.Name ?? "Empresa Confidencial";
        var publishedDate = DateTimeOffset.FromUnixTimeSeconds(attributes?.PublishedAt ?? 0).UtcDateTime;

        // SOLUCIÓN: GetOnBoard manda la url en el nodo Links, o la podemos armar con el ID
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

    private record GetOnBoardResponse([property: JsonPropertyName("data")] List<GetOnBoardJob> Data);

    private record GetOnBoardJob(
        [property: JsonPropertyName("id")] string Id,
        [property: JsonPropertyName("attributes")] GetOnBoardAttributes Attributes,
        [property: JsonPropertyName("links")] GetOnBoardLinks Links
    );

    private record GetOnBoardLinks([property: JsonPropertyName("public_url")] string PublicUrl);

    private record GetOnBoardAttributes(
        [property: JsonPropertyName("title")] string Title,
        [property: JsonPropertyName("description")] string Description,
        [property: JsonPropertyName("remote")] bool Remote,
        [property: JsonPropertyName("published_at")] long PublishedAt,
        [property: JsonPropertyName("company")] GetOnBoardCompanyWrapper Company
    );

    private record GetOnBoardCompanyWrapper([property: JsonPropertyName("data")] GetOnBoardCompanyData Data);
    private record GetOnBoardCompanyData([property: JsonPropertyName("attributes")] GetOnBoardCompanyAttributes Attributes);
    private record GetOnBoardCompanyAttributes([property: JsonPropertyName("name")] string Name);
}