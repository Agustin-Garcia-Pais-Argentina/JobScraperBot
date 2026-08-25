using HtmlAgilityPack;
using JobScraperBot.Core.Models;
using Microsoft.Extensions.Logging;

namespace JobScraperBot.Scrapers;

/// <summary>
/// Parseo del HTML de WeRemoto separado de la llamada HTTP, para poder
/// testearlo contra un fixture guardado sin pegarle al sitio real.
/// </summary>
public static class WeRemotoMapper
{
    public const string SiteName = "WeRemoto";

    public static IReadOnlyList<JobOffer> MapListingHtml(string html, ILogger? logger = null)
    {
        var doc = new HtmlDocument();
        doc.LoadHtml(html);

        var offers = new List<JobOffer>();
        var articles = doc.DocumentNode.SelectNodes("//article");

        if (articles is null || articles.Count == 0)
        {
            logger?.LogWarning("[{Site}] No se encontraron etiquetas <article>. El HTML pudo haber cambiado.", SiteName);
            return offers;
        }

        foreach (var article in articles)
        {
            var offer = TryMapArticle(article);
            if (offer is not null) offers.Add(offer);
        }

        return offers;
    }

    private static JobOffer? TryMapArticle(HtmlNode article)
    {
        var linkNode = article.SelectSingleNode(".//a[contains(@href, '/job-posts/')]");
        if (linkNode is null) return null;

        var href = linkNode.GetAttributeValue("href", "");
        var fullUrl = href.StartsWith("http") ? href : $"https://www.weremoto.com{href}";
        var externalId = href.Split('/').LastOrDefault() ?? Guid.NewGuid().ToString();

        // Si no hay h2/h3, usamos el slug de la URL como título (mejor que nada).
        var titleNode = article.SelectSingleNode(".//h2") ?? article.SelectSingleNode(".//h3");
        var title = titleNode?.InnerText?.Trim()
                    ?? externalId.Replace("-", " ").Replace("id ", "").ToUpper();

        return new JobOffer(
            SourceSite: SiteName,
            ExternalId: externalId,
            Title: title,
            Company: "WeRemoto", // TODO: pulir cuando identifiquemos la clase CSS real de la empresa
            Location: "Remote (LATAM)",
            IsRemote: true,
            Url: fullUrl,
            PublishedAt: DateTime.UtcNow,
            Salary: null,
            RawDescription: article.InnerText.Trim()
        );
    }
}