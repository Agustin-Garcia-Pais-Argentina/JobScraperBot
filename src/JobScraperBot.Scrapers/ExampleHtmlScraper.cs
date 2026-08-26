using JobScraperBot.Core.Interfaces;
using JobScraperBot.Core.Models;
using Microsoft.Extensions.Logging;

namespace JobScraperBot.Scrapers;

/// <summary>
/// PLANTILLA para scrapers de sitios que exponen HTML estático (sin JS).
/// Para agregar un sitio nuevo: copiar esta clase, renombrarla, cambiar
/// SiteName y la URL, y ajustar los selectores en HtmlOfferMapper según
/// la estructura real del sitio. Si el sitio renderiza con JavaScript (SPA),
/// reemplazar el fetch de HttpClient por Playwright -- el resto del patrón
/// (IJobScraper + mapper testeable) se mantiene igual.
/// NO se registra en Program.cs por defecto: es un punto de partida, no un
/// scraper productivo.
/// </summary>
public class ExampleHtmlScraper : IJobScraper
{
    private readonly HttpClient _http;
    private readonly ILogger<ExampleHtmlScraper> _logger;

    public string SiteName => "SitioEjemplo"; // TODO: renombrar

    public ExampleHtmlScraper(HttpClient http, ILogger<ExampleHtmlScraper> logger)
    {
        _http = http;
        _logger = logger;
    }

    public async Task<IReadOnlyList<JobOffer>> ScrapeAsync(CancellationToken ct)
    {
        // TODO: reemplazar por la URL real de listado de empleos del sitio
        const string listingUrl = "https://example.com/empleos";

        var html = await _http.GetStringAsync(listingUrl, ct);
        return await HtmlOfferMapper.MapListingHtmlAsync(html, SiteName, _logger);
    }
}