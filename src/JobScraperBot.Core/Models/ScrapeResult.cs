namespace JobScraperBot.Core.Models;

public record ScrapeResult(
    string SiteName,
    bool WasSuccessful,
    IReadOnlyList<JobOffer> Offers,
    string? ErrorMessage = null
);
