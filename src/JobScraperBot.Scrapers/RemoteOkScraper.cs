using JobScraperBot.Core.Interfaces;
using JobScraperBot.Core.Models;
using Microsoft.Extensions.Logging;

namespace JobScraperBot.Scrapers;

public class RemoteOkScraper : IJobScraper
{
    private readonly HttpClient _http;
    private readonly ILogger<RemoteOkScraper> _logger;

    public string SiteName => RemoteOkMapper.SiteName;

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

        var json = await _http.GetStringAsync("https://remoteok.com/api?tag=dev", ct);
        return RemoteOkMapper.MapJson(json, _logger);
    }
}