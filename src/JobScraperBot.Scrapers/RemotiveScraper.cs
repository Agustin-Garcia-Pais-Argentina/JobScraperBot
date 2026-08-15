using System.Net.Http.Json;
using JobScraperBot.Core.Interfaces;
using JobScraperBot.Core.Models;
using Microsoft.Extensions.Logging;

namespace JobScraperBot.Scrapers;

/// <summary>
/// Scraper funcional de ejemplo: consume la API JSON pública de Remotive
/// (https://remotive.com/api/remote-jobs) en vez de parsear HTML. Muestra
/// que IJobScraper no está atado a ningún método de extracción particular --
/// cada sitio resuelve internamente cómo obtener los datos, mientras
/// devuelva el modelo común JobOffer.
/// </summary>
public class RemotiveScraper : IJobScraper
{
    private readonly HttpClient _http;
    private readonly ILogger<RemotiveScraper> _logger;

    public string SiteName => "Remotive";

    public RemotiveScraper(HttpClient http, ILogger<RemotiveScraper> logger)
    {
        _http = http;
        _logger = logger;
    }

    public async Task<IReadOnlyList<JobOffer>> ScrapeAsync(CancellationToken ct)
    {
        var response = await _http.GetFromJsonAsync<RemotiveResponse>(
            "https://remotive.com/api/remote-jobs?category=software-dev", cancellationToken: ct);

        if (response?.Jobs is null)
        {
            _logger.LogWarning(
                "[{Site}] La respuesta no trajo ofertas -- posible cambio de contrato de la API", SiteName);
            return Array.Empty<JobOffer>();
        }

        return response.Jobs.Select(MapToJobOffer).ToList();
    }

    private JobOffer MapToJobOffer(RemotiveJob job) => new(
        SourceSite: SiteName,
        ExternalId: job.Id.ToString(),
        Title: job.Title ?? "",
        Company: job.CompanyName ?? "",
        Location: job.CandidateRequiredLocation ?? "",
        IsRemote: true, // Remotive es 100% remoto por definición del sitio
        Url: job.Url ?? "",
        PublishedAt: job.PublicationDate,
        Salary: string.IsNullOrWhiteSpace(job.Salary) ? null : job.Salary,
        RawDescription: job.Description ?? ""
    );

    private record RemotiveResponse(List<RemotiveJob> Jobs);

    private record RemotiveJob(
        int Id,
        string? Title,
        string? CompanyName,
        string? Url,
        DateTime PublicationDate,
        string? CandidateRequiredLocation,
        string? Salary,
        string? Description);
}
