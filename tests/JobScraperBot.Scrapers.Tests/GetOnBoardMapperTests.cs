using JobScraperBot.Scrapers;
using Xunit;

namespace JobScraperBot.Scrapers.Tests;

public class GetOnBoardMapperTests
{
    private static string LoadFixture(string name)
        => File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Fixtures", name));

    [Fact]
    public void MapJson_ParsesBothJobs()
    {
        var json = LoadFixture("getonboard-response.json");

        var offers = GetOnBoardMapper.MapJson(json);

        Assert.Equal(2, offers.Count);
    }

    [Fact]
    public void MapJson_ExtractsCompanyNameAndRemoteFlag()
    {
        var json = LoadFixture("getonboard-response.json");

        var offers = GetOnBoardMapper.MapJson(json);
        var first = offers[0];

        Assert.Equal("Backend Developer Jr", first.Title);
        Assert.Equal("Acme LATAM", first.Company);
        Assert.True(first.IsRemote);
        Assert.Equal("https://www.getonbrd.com/jobs/backend-developer-jr-acme-latam", first.Url);
    }

    [Fact]
    public void MapJson_MissingPublicUrl_FallsBackToIdBasedUrl()
    {
        var json = LoadFixture("getonboard-response.json");

        var offers = GetOnBoardMapper.MapJson(json);
        var second = offers[1];

        Assert.Equal("https://www.getonbrd.com/jobs/98766", second.Url);
        Assert.False(second.IsRemote);
    }

    [Fact]
    public void MapJson_EmptyData_ReturnsEmpty()
    {
        var offers = GetOnBoardMapper.MapJson("""{"data": []}""");

        Assert.Empty(offers);
    }

    [Fact]
    public void MapJson_InvalidJson_ReturnsEmptyWithoutThrowing()
    {
        var offers = GetOnBoardMapper.MapJson("esto no es json valido {{{");

        Assert.Empty(offers);
    }
}