using Xunit;

namespace JobScraperBot.Filters.Tests;

public class SeniorityExcludeFilterTests
{
    private readonly SeniorityExcludeFilter _filter = new();
    private readonly Core.Models.UserProfile _profile = TestProfileFactory.Build();

    [Fact]
    public void Matches_NoSeniorityMentioned_ReturnsTrue()
    {
        var offer = TestOfferFactory.Build(title: "Backend Developer", description: "Buscamos developer con C#");
        Assert.True(_filter.Matches(offer, _profile));
    }

    [Fact]
    public void Matches_ExplicitSenior_ReturnsFalse()
    {
        var offer = TestOfferFactory.Build(title: "Senior Backend Developer", description: "5+ años de experiencia");
        Assert.False(_filter.Matches(offer, _profile));
    }

    [Fact]
    public void Matches_ExplicitJunior_ReturnsTrue()
    {
        var offer = TestOfferFactory.Build(title: "Backend Developer Junior");
        Assert.True(_filter.Matches(offer, _profile));
    }
}
