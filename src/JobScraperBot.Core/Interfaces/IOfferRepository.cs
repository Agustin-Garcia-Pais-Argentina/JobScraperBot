using JobScraperBot.Core.Models;

namespace JobScraperBot.Core.Interfaces;

public interface IOfferRepository
{
    /// <summary>IDs ya vistos (Active o Closed) para un sitio -- usado para deduplicar.</summary>
    Task<HashSet<string>> GetKnownActiveOrClosedIdsAsync(string site, CancellationToken ct);

    Task MarkSeenAsync(IEnumerable<JobOffer> offers, CancellationToken ct);

    /// <summary>Marca como Gone lo que estaba Active y no volvió a aparecer en esta corrida.</summary>
    Task MarkGoneOffersAsync(string site, IEnumerable<string> idsSeenThisRun, CancellationToken ct);

    Task SaveAsync(CancellationToken ct);
}
