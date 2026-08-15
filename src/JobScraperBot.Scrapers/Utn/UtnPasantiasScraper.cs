using JobScraperBot.Core.Interfaces;
using JobScraperBot.Core.Models;
using Microsoft.Extensions.Logging;

namespace JobScraperBot.Scrapers.Utn;

/// <summary>
/// Scrapea las convocatorias de pasantías y filtra por carrera usando el
/// código del ícono que precede a cada convocatoria en el HTML (ISI =
/// Ingeniería en Sistemas de Información, TUTI = Tecnicatura en TI) --
/// sin necesidad de abrir el PDF. El PDF se manda tal cual, adjunto, en el
/// mensaje de Telegram (ver TelegramNotifier); acá no se descarga ni se lee.
/// </summary>
public class UtnPasantiasScraper : IJobScraper
{
    private const string ListingUrl = "https://www.frsf.utn.edu.ar/extension/pasantias";

    // TODO: sumar más códigos acá si en el futuro interesan otras carreras
    // (ej. "TUM" si en algún momento te interesa Mecatrónica).
    private static readonly HashSet<string> RelevantCareerCodes =
        new(StringComparer.OrdinalIgnoreCase) { "ISI", "TUTI" };

    private readonly HttpClient _http;
    private readonly ILogger<UtnPasantiasScraper> _logger;

    public string SiteName => UtnPasantiasHtmlMapper.SiteName;

    public UtnPasantiasScraper(HttpClient http, ILogger<UtnPasantiasScraper> logger)
    {
        _http = http;
        _logger = logger;
    }

    public async Task<IReadOnlyList<JobOffer>> ScrapeAsync(CancellationToken ct)
    {
        var html = await _http.GetStringAsync(ListingUrl, ct);
        var convocatorias = await UtnPasantiasHtmlMapper.ExtractConvocatoriasAsync(html, _logger);

        var relevant = convocatorias
            .Where(c => c.CareerCodes.Any(code => RelevantCareerCodes.Contains(code)))
            .ToList();

        _logger.LogInformation(
            "[{Site}] {Total} convocatoria(s) encontrada(s), {Relevant} de Sistemas/TI",
            SiteName, convocatorias.Count, relevant.Count);

        return relevant.Select(BuildOffer).ToList();
    }

    private JobOffer BuildOffer(PasantiaConvocatoria c) => new(
        SourceSite: SiteName,
        ExternalId: c.ReferenceCode,
        // El título arranca con "Pasantía" a propósito: garantiza que el
        // RoleProfile de Pasantías (que exige ese término) siempre matchee.
        Title: $"Pasantía - Convocatoria {c.ReferenceCode}",
        Company: "",
        Location: "",
        IsRemote: false,
        Url: c.PdfUrl, // el PDF se adjunta directo en el mensaje, ver TelegramNotifier
        PublishedAt: DateTime.UtcNow,
        Salary: null,
        RawDescription: $"Pasantía para {string.Join("/", c.CareerCodes)}. Detalle completo en el PDF adjunto."
    );
}