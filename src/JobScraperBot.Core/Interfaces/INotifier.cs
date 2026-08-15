using JobScraperBot.Core.Models;

namespace JobScraperBot.Core.Interfaces;

public interface INotifier
{
    Task SendSummaryAsync(IReadOnlyList<JobOffer> newOffers, CancellationToken ct);
}
