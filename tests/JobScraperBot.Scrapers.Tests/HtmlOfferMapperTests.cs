using JobScraperBot.Scrapers;
using Xunit;

namespace JobScraperBot.Scrapers.Tests;

/// <summary>
/// Testea el parseo contra un HTML guardado localmente (fixture), no contra
/// el sitio en vivo -- así detectamos rápido si un cambio de selectores
/// rompió el mapeo, sin depender de la disponibilidad del sitio real.
/// </summary>
public class HtmlOfferMapperTests
{
    private static string LoadFixture(string name)
        => File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Fixtures", name));

    [Fact]
    public async Task MapListingHtmlAsync_ParsesBothCards()
    {
        var html = LoadFixture("example-listing.html");

        var offers = await HtmlOfferMapper.MapListingHtmlAsync(html, "SitioEjemplo");

        Assert.Equal(2, offers.Count);
        Assert.Equal("Backend Developer Junior", offers[0].Title);
        Assert.Equal("Acme SRL", offers[0].Company);
        Assert.False(offers[0].IsRemote);
        Assert.True(offers[1].IsRemote); // "Remoto" está en el location
    }

    [Fact]
    public async Task MapListingHtmlAsync_NoCardsFound_ReturnsEmpty()
    {
        // Simula que el sitio cambió su HTML: el selector no encuentra nada
        var offers = await HtmlOfferMapper.MapListingHtmlAsync("<html><body>sin ofertas</body></html>", "SitioEjemplo");

        Assert.Empty(offers);
    }
}
