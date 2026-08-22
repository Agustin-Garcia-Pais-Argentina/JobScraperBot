using HtmlAgilityPack;
using JobScraperBot.Core.Interfaces;
using JobScraperBot.Core.Models;
using Microsoft.Extensions.Logging;

namespace JobScraperBot.Scrapers;

public class WeRemotoScraper : IJobScraper
{
    private readonly HttpClient _http;
    private readonly ILogger<WeRemotoScraper> _logger;

    public string SiteName => "WeRemoto";

    public WeRemotoScraper(HttpClient http, ILogger<WeRemotoScraper> logger)
    {
        _http = http;
        _logger = logger;
    }

    public async Task<IReadOnlyList<JobOffer>> ScrapeAsync(CancellationToken ct)
    {
        
        // 1. Nos disfrazamos de Google Chrome para que WeRemoto no nos bloquee
        if (!_http.DefaultRequestHeaders.Contains("User-Agent"))
        {
            _http.DefaultRequestHeaders.TryAddWithoutValidation(
                "User-Agent", 
                "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36");
        }
        
        
        // 2. PONÉ ACÁ LA URL EXACTA QUE VES EN TU NAVEGADOR
        var url = "https://www.weremoto.com/categoria-de-trabajo/programacion"; // <-- Cambiá esto si la URL de la categoría es distinta
        
        // 3. Descargamos el HTML crudo
        var html = await _http.GetStringAsync(url, ct);

        // 2. Cargamos el DOM con HtmlAgilityPack
        var doc = new HtmlDocument();
        doc.LoadHtml(html);

        var offers = new List<JobOffer>();

        // 3. Buscamos todas las etiquetas <article> que vimos en tu captura
        var articles = doc.DocumentNode.SelectNodes("//article");

        if (articles is null || articles.Count == 0)
        {
            _logger.LogWarning("[{Site}] No se encontraron etiquetas <article>. El HTML pudo haber cambiado.", SiteName);
            return offers;
        }

        foreach (var article in articles)
        {
            // Buscamos el link que lleva al detalle del trabajo
            var linkNode = article.SelectSingleNode(".//a[contains(@href, '/job-posts/')]");
            if (linkNode == null) continue;

            var href = linkNode.GetAttributeValue("href", "");
            var fullUrl = href.StartsWith("http") ? href : $"https://www.weremoto.com{href}";
            
            // Extraemos el ID único de la URL (ej: "id-full-stack-product-engineer-...")
            var externalId = href.Split('/').LastOrDefault() ?? Guid.NewGuid().ToString();

            // Como los nodos de título/empresa estaban colapsados en la foto, 
            // intentamos buscar un H2 o H3. Si no está, limpiamos el slug de la URL para usarlo de título.
            var titleNode = article.SelectSingleNode(".//h2") ?? article.SelectSingleNode(".//h3");
            var title = titleNode?.InnerText?.Trim() 
                        ?? externalId.Replace("-", " ").Replace("id ", "").ToUpper();

            offers.Add(new JobOffer(
                SourceSite: SiteName,
                ExternalId: externalId,
                Title: title,
                Company: "WeRemoto", // Se puede pulir después si identificamos la clase CSS
                Location: "Remote (LATAM)",
                IsRemote: true,
                Url: fullUrl,
                PublishedAt: DateTime.UtcNow,
                Salary: null,
                RawDescription: article.InnerText.Trim() // Guardamos todo el texto del article para que el filtro actúe
            ));
        }

        return offers;
    }
}