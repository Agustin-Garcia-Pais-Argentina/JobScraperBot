using JobScraperBot.Core.Interfaces;
using JobScraperBot.Core.Models;
using Microsoft.Extensions.Logging;

namespace JobScraperBot.Scrapers.Utn;

public class UtnBusquedasLaboralesScraper : IJobScraper
{
    private const string ListingUrl = "https://www.frsf.utn.edu.ar/graduados/busquedas-laborales";

    private readonly HttpClient _http;
    private readonly ILogger<UtnBusquedasLaboralesScraper> _logger;

    public string SiteName => UtnBusquedasLaboralesMapper.SiteName;

    public UtnBusquedasLaboralesScraper(HttpClient http, ILogger<UtnBusquedasLaboralesScraper> logger)
    {
        _http = http;
        _logger = logger;
    }

    public async Task<IReadOnlyList<JobOffer>> ScrapeAsync(CancellationToken ct)
    {
        var html = await _http.GetStringAsync(ListingUrl, ct);
        return await UtnBusquedasLaboralesMapper.MapListingHtmlAsync(html, _logger);
    }
}