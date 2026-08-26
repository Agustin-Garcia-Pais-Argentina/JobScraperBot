using JobScraperBot.Scrapers;
using Xunit;

namespace JobScraperBot.Scrapers.Tests;

public class RemoteOkMapperTests
{
    private static string LoadFixture(string name)
        => File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Fixtures", name));

    [Fact]
    public void MapJson_SkipsDisclaimerAndMapsRealJobs()
    {
        var json = LoadFixture("remoteok-response.json");

        var offers = RemoteOkMapper.MapJson(json);

        Assert.Equal(2, offers.Count);
        Assert.Equal("Backend Developer", offers[0].Title);
        Assert.Equal("Acme Corp", offers[0].Company);
        Assert.True(offers[0].IsRemote);
        Assert.Equal("12345", offers[0].ExternalId);
    }

    [Fact]
    public void MapJson_EmptyArray_ReturnsEmpty()
    {
        var offers = RemoteOkMapper.MapJson("[]");

        Assert.Empty(offers);
    }

    [Fact]
    public void MapJson_OnlyDisclaimer_ReturnsEmpty()
    {
        var offers = RemoteOkMapper.MapJson("""[{"legal": "disclaimer"}]""");

        Assert.Empty(offers);
    }

    [Fact]
    public void MapJson_InvalidJson_ReturnsEmptyWithoutThrowing()
    {
        var offers = RemoteOkMapper.MapJson("esto no es json valido {{{");

        Assert.Empty(offers);
    }
}