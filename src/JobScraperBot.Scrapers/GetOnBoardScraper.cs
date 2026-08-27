using System.Net;
using JobScraperBot.Core.Interfaces;
using JobScraperBot.Core.Models;
using JobScraperBot.Infrastructure.Resilience;
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
        using var request = new HttpRequestMessage(HttpMethod.Get, url);
        using var response = await _http.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, ct);

        if (response.StatusCode == HttpStatusCode.TooManyRequests)
        {
            var retryAfter = ResiliencePolicies.ParseRetryAfter(response.Headers);
            throw new RetryableScrapeException(
                $"[{SiteName}] rate limited by GetOnBoard",
                response.StatusCode,
                retryAfter ?? TimeSpan.FromSeconds(2));
        }

        if ((int)response.StatusCode >= 500)
            throw new RetryableScrapeException($"[{SiteName}] servidor respondió {response.StatusCode}", response.StatusCode);

        if (response.StatusCode == HttpStatusCode.NotFound)
            throw new PermanentScrapeException($"[{SiteName}] no se encontró la API", response.StatusCode);

        if (!response.IsSuccessStatusCode)
            throw new PermanentScrapeException($"[{SiteName}] respuesta HTTP {(int)response.StatusCode}", response.StatusCode);

        var json = await response.Content.ReadAsStringAsync(ct);
        return GetOnBoardMapper.MapJson(json, _logger);
    }
}