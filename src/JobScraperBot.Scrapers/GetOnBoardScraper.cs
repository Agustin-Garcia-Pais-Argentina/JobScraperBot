using JobScraperBot.Core.Interfaces;
using JobScraperBot.Core.Models;
using Microsoft.Extensions.Logging;

namespace JobScraperBot.Scrapers;

public class GetOnBoardScraper : IJobScraper
{
    private readonly HttpClient _http;
    private readonly ILogger<GetOnBoardScraper> _logger;

    public string SiteName => GetOnBoardMapper.SiteName;

    public GetOnBoardScraper(HttpClient http, ILogger<GetOnBoardScraper> logger)
    {
        _http = http;
        _logger = logger;
    }

    public async Task<IReadOnlyList<JobOffer>> ScrapeAsync(CancellationToken ct)
    {
        var url = "https://www.getonbrd.com/api/v0/categories/programming/jobs?per_page=50&expand=[\"company\"]";
        var json = await _http.GetStringAsync(url, ct);
        return GetOnBoardMapper.MapJson(json, _logger);
    }
}