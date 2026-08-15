using JobScraperBot.Scrapers.Utn;
using Xunit;

namespace JobScraperBot.Scrapers.Tests;

public class UtnPasantiasHtmlMapperTests
{
    private static string LoadFixture(string name)
        => File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Fixtures", name));

    [Fact]
    public async Task ExtractConvocatoriasAsync_FindsBothConvocatorias()
    {
        var html = LoadFixture("utn-pasantias.html");

        var convocatorias = await UtnPasantiasHtmlMapper.ExtractConvocatoriasAsync(html);

        Assert.Equal(2, convocatorias.Count);
    }

    [Fact]
    public async Task ExtractConvocatoriasAsync_AssignsCareerIconsByTextOrder()
    {
        var html = LoadFixture("utn-pasantias.html");

        var convocatorias = await UtnPasantiasHtmlMapper.ExtractConvocatoriasAsync(html);

        var first = convocatorias[0];
        Assert.Equal("CN Q 06_08 TAC", first.ReferenceCode);
        Assert.Equal(new[] { "IC", "IEE" }, first.CareerCodes);

        var second = convocatorias[1];
        Assert.Equal("CN K 30_07_EPE OyM", second.ReferenceCode);
        Assert.Equal(new[] { "II", "IM", "ISI" }, second.CareerCodes);
    }

    [Fact]
    public async Task ExtractConvocatoriasAsync_ResolvesRelativeUrlToAbsolute()
    {
        var html = LoadFixture("utn-pasantias.html");

        var convocatorias = await UtnPasantiasHtmlMapper.ExtractConvocatoriasAsync(html);

        Assert.Equal(
            "https://www.frsf.utn.edu.ar/images/lamherdtfrsf.utn.edu.ar/CN_Q_06_08_TAC.pdf",
            convocatorias[0].PdfUrl);
    }

    [Fact]
    public async Task ExtractConvocatoriasAsync_TrailingIconsWithoutConvocatoria_AreIgnored()
    {
        // TUTI y TUM quedan sin convocatoria asociada al final del fixture --
        // no deberían generar una entrada fantasma ni romper el parseo.
        var html = LoadFixture("utn-pasantias.html");

        var convocatorias = await UtnPasantiasHtmlMapper.ExtractConvocatoriasAsync(html);

        Assert.DoesNotContain(convocatorias, c => c.CareerCodes.Contains("TUTI"));
        Assert.DoesNotContain(convocatorias, c => c.CareerCodes.Contains("TUM"));
    }

    [Fact]
    public async Task ExtractConvocatoriasAsync_HandlesNonBreakingSpaceBetweenWords()
    {
        var html = "<h2>CONVOCATORIAS&nbsp;ABIERTAS</h2>" +
                    "<img src=\"/x/ISI.png\" alt=\"ISI\" />" +
                    "<h4>CONVOCATORIA&nbsp;REF <a href=\"/images/test.pdf\">CN 99</a></h4>";

        var convocatorias = await UtnPasantiasHtmlMapper.ExtractConvocatoriasAsync(html);

        Assert.Single(convocatorias);
        Assert.Equal("CN 99", convocatorias[0].ReferenceCode);
        Assert.Equal(new[] { "ISI" }, convocatorias[0].CareerCodes);
    }

    [Fact]
    public async Task ExtractConvocatoriasAsync_HandlesWhitespaceOrTagsBetweenRefAndLink()
    {
        var html = "<h2>CONVOCATORIAS ABIERTAS</h2>" +
                    "<h4>CONVOCATORIA REF\n  <a href=\"/images/x.pdf\">CN 1</a></h4>";

        var convocatorias = await UtnPasantiasHtmlMapper.ExtractConvocatoriasAsync(html);

        Assert.Single(convocatorias);
        Assert.Equal("CN 1", convocatorias[0].ReferenceCode);
    }

    [Fact]
    public async Task ExtractConvocatoriasAsync_SectionMissing_ReturnsEmpty()
    {
        var convocatorias = await UtnPasantiasHtmlMapper.ExtractConvocatoriasAsync(
            "<html><body><p>Sin convocatorias por ahora.</p></body></html>");

        Assert.Empty(convocatorias);
    }

    [Fact]
    public async Task ExtractConvocatoriasAsync_SectionFoundButNoConvocatorias_ReturnsEmpty()
    {
        var convocatorias = await UtnPasantiasHtmlMapper.ExtractConvocatoriasAsync(
            "<h2>CONVOCATORIAS ABIERTAS</h2><p>No hay convocatorias abiertas en este momento.</p>");

        Assert.Empty(convocatorias);
    }
}