using JobScraperBot.Core.Models;
using Xunit;

namespace JobScraperBot.Filters.Tests;

public class CompanyExcludeFilterTests
{
    private readonly CompanyExcludeFilter _filter = new();

    [Fact]
    public void Matches_NoExcludedCompanies_ReturnsTrue()
    {
        var profile = new UserProfile { ExcludedCompanies = new() };
        var offer = TestOfferFactory.Build(title: "Backend Developer");

        Assert.True(_filter.Matches(offer, profile));
    }

    [Fact]
    public void Matches_CompanyInExcludedList_ReturnsFalse()
    {
        // TestOfferFactory.Build() usa "Empresa Test" como Company por defecto.
        var profile = new UserProfile { ExcludedCompanies = new() { "Empresa Test" } };
        var offer = TestOfferFactory.Build(title: "Backend Developer");

        Assert.False(_filter.Matches(offer, profile));
    }

    [Fact]
    public void Matches_ComparisonIsCaseInsensitive()
    {
        var profile = new UserProfile { ExcludedCompanies = new() { "empresa test" } };
        var offer = TestOfferFactory.Build(title: "Backend Developer");

        Assert.False(_filter.Matches(offer, profile));
    }

    [Fact]
    public void Matches_CompanyNotInList_ReturnsTrue()
    {
        var profile = new UserProfile { ExcludedCompanies = new() { "Otra Empresa SRL" } };
        var offer = TestOfferFactory.Build(title: "Backend Developer");

        Assert.True(_filter.Matches(offer, profile));
    }
}