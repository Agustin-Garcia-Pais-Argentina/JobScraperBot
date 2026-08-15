using AngleSharp;
using AngleSharp.Dom;
using JobScraperBot.Core.Models;
using Microsoft.Extensions.Logging;

namespace JobScraperBot.Scrapers;

/// <summary>
/// Lógica de parseo de HTML separada del scraper a propósito, para poder
/// testearla contra un archivo HTML guardado (fixture) sin depender de una
/// llamada HTTP real ni de la disponibilidad del sitio (ver
/// tests/JobScraperBot.Scrapers.Tests).
/// </summary>
public static class HtmlOfferMapper
{
    public static async Task<IReadOnlyList<JobOffer>> MapListingHtmlAsync(
        string html, string siteName, ILogger? logger = null)
    {
        using var context = BrowsingContext.New(Configuration.Default);
        var document = await context.OpenAsync(req => req.Content(html));

        // TODO: reemplazar ".job-card" por el selector real de cada tarjeta de oferta
        var cards = document.QuerySelectorAll(".job-card");

        if (cards.Length == 0)
        {
            logger?.LogWarning(
                "[{Site}] El selector .job-card no encontró resultados -- el sitio puede haber cambiado su HTML",
                siteName);
            return Array.Empty<JobOffer>();
        }

        var offers = new List<JobOffer>();
        foreach (var card in cards)
        {
            var offer = TryMapCard(card, siteName, logger);
            if (offer is not null) offers.Add(offer);
        }

        return offers;
    }

    private static JobOffer? TryMapCard(IElement card, string siteName, ILogger? logger)
    {
        try
        {
            // TODO: ajustar todos estos selectores según el sitio real (inspeccionar con DevTools)
            var title = card.QuerySelector(".job-title")?.TextContent.Trim() ?? "";
            var company = card.QuerySelector(".job-company")?.TextContent.Trim() ?? "";
            var location = card.QuerySelector(".job-location")?.TextContent.Trim() ?? "";
            var url = card.QuerySelector("a")?.GetAttribute("href") ?? "";
            var externalId = url.Split('/').LastOrDefault() ?? Guid.NewGuid().ToString();
            var isRemote = location.Contains("remoto", StringComparison.OrdinalIgnoreCase);
            var description = card.QuerySelector(".job-description")?.TextContent.Trim() ?? "";

            if (string.IsNullOrWhiteSpace(title))
                return null;

            return new JobOffer(
                SourceSite: siteName,
                ExternalId: externalId,
                Title: title,
                Company: company,
                Location: location,
                IsRemote: isRemote,
                Url: url,
                PublishedAt: DateTime.UtcNow, // TODO: parsear fecha real si el sitio la da
                Salary: null,                 // TODO: extraer salario si el sitio lo informa
                RawDescription: description
            );
        }
        catch (Exception ex)
        {
            logger?.LogWarning(ex, "[{Site}] Error mapeando una tarjeta de oferta, se omite", siteName);
            return null;
        }
    }
}
