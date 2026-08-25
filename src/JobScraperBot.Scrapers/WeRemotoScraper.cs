using JobScraperBot.Core.Interfaces;
using JobScraperBot.Core.Models;
using Microsoft.Extensions.Logging;

namespace JobScraperBot.Scrapers;

public class WeRemotoScraper : IJobScraper
{
    private readonly HttpClient _http;
    private readonly ILogger<WeRemotoScraper> _logger;

    public string SiteName => WeRemotoMapper.SiteName;

    public WeRemotoScraper(HttpClient http, ILogger<WeRemotoScraper> logger)
    {
        _http = http;
        _logger = logger;
    }

    public async Task<IReadOnlyList<JobOffer>> ScrapeAsync(CancellationToken ct)
    {
        // User-Agent honesto (no disfrazado de navegador) -- WeRemoto permite
        // crawling explícitamente (meta-robots: index, follow), así que alcanza
        // con identificarnos, sin necesidad de simular ser Chrome.
        if (!_http.DefaultRequestHeaders.Contains("User-Agent"))
        {
            _http.DefaultRequestHeaders.TryAddWithoutValidation("User-Agent", "JobScraperBot/1.0");
        }

        var url = "https://www.weremoto.com/categoria-de-trabajo/programacion";
        var html = await _http.GetStringAsync(url, ct);
        return WeRemotoMapper.MapListingHtml(html, _logger);
    }
}