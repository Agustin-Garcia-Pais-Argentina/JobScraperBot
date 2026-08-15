using JobScraperBot.Core.Models;

namespace JobScraperBot.Filters.Tests;

internal static class TestOfferFactory
{
    public static JobOffer Build(
        string title = "Developer",
        string description = "",
        string location = "",
        bool isRemote = false,
        string site = "TestSite",
        string id = "1")
        => new(
            SourceSite: site,
            ExternalId: id,
            Title: title,
            Company: "Empresa Test",
            Location: location,
            IsRemote: isRemote,
            Url: "https://example.com/job/1",
            PublishedAt: DateTime.UtcNow,
            Salary: null,
            RawDescription: description);
}
