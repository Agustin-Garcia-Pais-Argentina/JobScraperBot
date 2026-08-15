using JobScraperBot.Core.Models;

namespace JobScraperBot.Core.Interfaces;

/// <summary>
/// Contrato que debe cumplir cada scraper "plugin" de un sitio. No importa
/// cómo obtiene los datos por dentro (HTML estático, JS con Playwright, o
/// una API JSON pública) -- el resto del sistema solo conoce esta interfaz.
/// </summary>
public interface IJobScraper
{
    string SiteName { get; }
    Task<IReadOnlyList<JobOffer>> ScrapeAsync(CancellationToken ct);
}
