using Xunit;

namespace JobScraperBot.Filters.Tests;

public class RoleFilterTests
{
    private readonly RoleFilter _filter = new();
    private readonly Core.Models.UserProfile _profile = TestProfileFactory.Build();

    [Fact]
    public void Matches_BackendDeveloperOffer_ReturnsTrue()
    {
        var offer = TestOfferFactory.Build(title: "Backend Developer Junior", description: "Buscamos backend developer con C#");
        Assert.True(_filter.Matches(offer, _profile));
    }

    [Fact]
    public void Matches_TechSupportInternship_ReturnsFalse()
    {
        var offer = TestOfferFactory.Build(title: "Pasantía Soporte Técnico", description: "Pasantía para el área de soporte técnico");
        Assert.False(_filter.Matches(offer, _profile));
    }

    [Fact]
    public void Matches_NonTechInternship_ReturnsTrue()
    {
        // Pasantías sin filtro de área -> pasa aunque no sea rol técnico, mientras no sea soporte
        var offer = TestOfferFactory.Build(title: "Pasantía Marketing", description: "Pasantía en el área de marketing digital");
        Assert.True(_filter.Matches(offer, _profile));
    }

    [Fact]
    public void Matches_UnrelatedRole_ReturnsFalse()
    {
        var offer = TestOfferFactory.Build(title: "Vendedor", description: "Se busca vendedor para local comercial");
        Assert.False(_filter.Matches(offer, _profile));
    }
}
