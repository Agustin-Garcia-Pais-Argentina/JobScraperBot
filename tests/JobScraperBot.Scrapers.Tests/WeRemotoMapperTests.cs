using JobScraperBot.Scrapers;
using Xunit;

namespace JobScraperBot.Scrapers.Tests;

public class WeRemotoMapperTests
{
    private static string LoadFixture(string name)
        => File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Fixtures", name));

    [Fact]
    public void MapListingHtml_IgnoresArticlesWithoutJobLink()
    {
        var html = LoadFixture("weremoto-listing.html");

        var offers = WeRemotoMapper.MapListingHtml(html);

        // El 3er <article> no tiene link a /job-posts/ y debe ignorarse.
        Assert.Equal(2, offers.Count);
    }

    [Fact]
    public void MapListingHtml_UsesH2TitleWhenPresent()
    {
        var html = LoadFixture("weremoto-listing.html");

        var offers = WeRemotoMapper.MapListingHtml(html);

        Assert.Equal("Backend Developer LATAM", offers[0].Title);
        Assert.Equal("id-backend-developer-latam-acme", offers[0].ExternalId);
    }

    [Fact]
    public void MapListingHtml_FallsBackToSlugTitleWhenNoHeading()
    {
        var html = LoadFixture("weremoto-listing.html");

        var offers = WeRemotoMapper.MapListingHtml(html);

        // El 2do article no tiene <h2>/<h3> -- el título sale del slug de la URL.
        Assert.Contains("SENIOR FULLSTACK ENGINEER BETA", offers[1].Title);
    }

    [Fact]
    public void MapListingHtml_NoArticles_ReturnsEmpty()
    {
        var offers = WeRemotoMapper.MapListingHtml("<html><body>sin ofertas</body></html>");

        Assert.Empty(offers);
    }
}