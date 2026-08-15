using Xunit;

namespace JobScraperBot.Filters.Tests;

public class LocationFilterTests
{
    private readonly LocationFilter _filter = new();
    private readonly Core.Models.UserProfile _profile = TestProfileFactory.Build();

    [Fact]
    public void Matches_Remote_AlwaysTrue()
    {
        var offer = TestOfferFactory.Build(location: "Rosario", isRemote: true);
        Assert.True(_filter.Matches(offer, _profile));
    }

    [Fact]
    public void Matches_OnsiteSantaFeCapital_ReturnsTrue()
    {
        var offer = TestOfferFactory.Build(location: "Santa Fe Capital", isRemote: false);
        Assert.True(_filter.Matches(offer, _profile));
    }

    [Fact]
    public void Matches_OnsiteCaba_ReturnsFalse()
    {
        var offer = TestOfferFactory.Build(location: "CABA", isRemote: false);
        Assert.False(_filter.Matches(offer, _profile));
    }

    [Fact]
    public void Matches_OnsiteAmbiguousLocation_ReturnsTrue()
    {
        // No aclara ciudad -> ante la duda, se incluye
        var offer = TestOfferFactory.Build(location: "Argentina", isRemote: false);
        Assert.True(_filter.Matches(offer, _profile));
    }
}
