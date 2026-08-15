using JobScraperBot.Core.Interfaces;
using JobScraperBot.Core.Models;
using JobScraperBot.Filters;
using JobScraperBot.Infrastructure.Resilience;
using Microsoft.Extensions.Logging;

namespace JobScraperBot.Orchestration;

/// <summary>
/// No conoce HTML ni sitios específicos: solo sabe que existe una lista de
/// IJobScraper. Corre todos en paralelo, aislando errores por sitio, y
/// coordina filtrado -> deduplicación -> notificación -> persistencia.
/// </summary>
public class ScrapeOrchestrator
{
    private readonly IEnumerable<IJobScraper> _scrapers;
    private readonly FilterPipeline _filterPipeline;
    private readonly IOfferRepository _repository;
    private readonly INotifier _notifier;
    private readonly ILogger<ScrapeOrchestrator> _logger;

    public ScrapeOrchestrator(
        IEnumerable<IJobScraper> scrapers,
        FilterPipeline filterPipeline,
        IOfferRepository repository,
        INotifier notifier,
        ILogger<ScrapeOrchestrator> logger)
    {
        _scrapers = scrapers;
        _filterPipeline = filterPipeline;
        _repository = repository;
        _notifier = notifier;
        _logger = logger;
    }

    public async Task RunFullCycleAsync(UserProfile profile, CancellationToken ct)
    {
        var scrapeResults = await Task.WhenAll(_scrapers.Select(s => SafeScrapeAsync(s, ct)));
        var allNewOffers = new List<JobOffer>();

        foreach (var result in scrapeResults)
        {
            if (!result.WasSuccessful)
            {
                _logger.LogWarning("[{Site}] Scraping falló, se omite esta corrida: {Error}", result.SiteName, result.ErrorMessage);
                continue;
            }

            var known = await _repository.GetKnownActiveOrClosedIdsAsync(result.SiteName, ct);
            var filtered = _filterPipeline.Apply(result.Offers, profile).ToList();
            var newOnes = filtered.Where(o => !known.Contains(o.ExternalId)).ToList();

            allNewOffers.AddRange(newOnes);

            await _repository.MarkSeenAsync(filtered, ct);

            // Solo marcamos Gone si el scraping de este sitio trajo resultados:
            // un sitio caído no es lo mismo que ofertas que desaparecieron.
            if (result.Offers.Count > 0)
                await _repository.MarkGoneOffersAsync(result.SiteName, result.Offers.Select(o => o.ExternalId), ct);
        }

        await _repository.SaveAsync(ct);

        _logger.LogInformation("Ciclo completo: {Count} ofertas nuevas para notificar", allNewOffers.Count);
        await _notifier.SendSummaryAsync(allNewOffers, ct);
    }

    private async Task<ScrapeResult> SafeScrapeAsync(IJobScraper scraper, CancellationToken ct)
    {
        var retryPolicy = ResiliencePolicies.CreateScraperRetryPolicy(
            (ex, attempt) => _logger.LogWarning(
                "[{Site}] Reintento {Attempt} tras error: {Message}", scraper.SiteName, attempt, ex.Message));

        try
        {
            var offers = await retryPolicy.ExecuteAsync(() => scraper.ScrapeAsync(ct));
            return new ScrapeResult(scraper.SiteName, true, offers);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[{Site}] Scraping falló definitivamente tras reintentos", scraper.SiteName);
            return new ScrapeResult(scraper.SiteName, false, Array.Empty<JobOffer>(), ex.Message);
        }
    }
}
