using JobScraperBot.Scrapers.Utn;
using Xunit;

namespace JobScraperBot.Scrapers.Tests;

public class UtnBusquedasLaboralesMapperTests
{
    private static string LoadFixture(string name)
        => File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Fixtures", name));

    [Fact]
    public async Task MapListingHtmlAsync_ParsesAllThreeBusquedas()
    {
        var html = LoadFixture("utn-busquedas-laborales.html");

        var offers = await UtnBusquedasLaboralesMapper.MapListingHtmlAsync(html);

        Assert.Equal(3, offers.Count);
    }

    [Fact]
    public async Task MapListingHtmlAsync_ExtractsFieldsCorrectly()
    {
        var html = LoadFixture("utn-busquedas-laborales.html");

        var offers = await UtnBusquedasLaboralesMapper.MapListingHtmlAsync(html);
        var first = offers.Single(o => o.ExternalId == "26036");

        Assert.Equal("Joven Profesional", first.Title);
        Assert.Equal("Pluspetrol", first.Company);
        Assert.Contains("Ing. Mecánica", first.RawDescription);
        Assert.Contains("8 de Agosto de 2026", first.RawDescription);
        Assert.Contains("26036_Pluspetrol.pdf", first.Url);
        Assert.False(first.IsRemote);
    }

    [Fact]
    public async Task MapListingHtmlAsync_NoBusquedasFound_ReturnsEmpty()
    {
        var offers = await UtnBusquedasLaboralesMapper.MapListingHtmlAsync(
            "<html><body><div itemprop='articleBody'><p>Sin búsquedas activas por el momento.</p></div></body></html>");

        Assert.Empty(offers);
    }

    [Fact]
    public async Task MapListingHtmlAsync_ArticleBodyMissing_ReturnsEmptyAndDoesNotThrow()
    {
        var offers = await UtnBusquedasLaboralesMapper.MapListingHtmlAsync(
            "<html><body><p>Plantilla completamente distinta</p></body></html>");

        Assert.Empty(offers);
    }
}
