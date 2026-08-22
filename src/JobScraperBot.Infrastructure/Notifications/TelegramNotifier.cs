using System.Text;
using JobScraperBot.Core.Interfaces;
using JobScraperBot.Core.Models;
using Microsoft.Extensions.Logging;
using Telegram.Bot;
using Telegram.Bot.Types;

namespace JobScraperBot.Infrastructure.Notifications;

/// <summary>
/// Ofertas cuyo Url termina en .pdf se mandan como documento adjunto
/// (SendDocument) -- así se lee el PDF original en vez de depender de un
/// resumen armado por el bot. El resto de las ofertas van agrupadas en el
/// mensaje de texto de siempre.
/// </summary>
public class TelegramNotifier : INotifier
{
    private readonly TelegramBotClient _bot;
    private readonly HttpClient _http;
    private readonly string _chatId;
    private readonly ILogger<TelegramNotifier> _logger;

    public TelegramNotifier(string botToken, string chatId, HttpClient http, ILogger<TelegramNotifier> logger)
    {
        _bot = new TelegramBotClient(botToken);
        _http = http;
        _chatId = chatId;
        _logger = logger;
    }

    public async Task SendSummaryAsync(IReadOnlyList<JobOffer> newOffers, CancellationToken ct)
{
    if (!newOffers.Any())
    {
        _logger.LogInformation("Sin ofertas nuevas, no se envía mensaje.");
        return;
    }

    // 1. Agrupamos todas las ofertas según la página de origen
    var groupedOffers = newOffers.GroupBy(o => o.SourceSite);

    // 2. Iteramos sobre cada grupo (cada página)
    foreach (var group in groupedOffers)
    {
        var siteName = group.Key;
        var siteOffers = group.ToList();

        // 3. Armamos el título exclusivo para este mensaje
        var message = $"🔎 {siteOffers.Count} nuevas ofertas en — {siteName} —\n\n";

        foreach (var offer in siteOffers)
        {
            message += $"• {offer.Title} — {offer.Company}\n  {offer.Url}\n";
        }

        try
        {
            // 4. Enviamos el mensaje individual para esta página
            await _bot.SendMessage(
                chatId: _chatId,
                text: message,
                linkPreviewOptions: new LinkPreviewOptions
                {
                    IsDisabled = true
                },
                cancellationToken: ct
            );
            
            // Le damos un respiro de medio segundo a la API de Telegram para que no nos bloquee por flood
            await Task.Delay(500, ct); 
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Fallo al enviar el resumen de la página {Site}", siteName);
        }
    }
}

    private async Task SendPdfAsync(JobOffer offer, CancellationToken ct)
    {
        try
        {
            var pdfBytes = await _http.GetByteArrayAsync(offer.Url, ct);
            var fileName = offer.Url.Split('/').LastOrDefault() ?? $"{offer.ExternalId}.pdf";
            var caption = $"📄 {offer.SourceSite}\n{offer.Title}";

            using var stream = new MemoryStream(pdfBytes);
            await _bot.SendDocument(_chatId, InputFile.FromStream(stream, fileName), caption, cancellationToken: ct);
        }
        catch (Exception ex)
        {
            // Si falla la descarga/envío del PDF, al menos mandamos el link
            // por texto -- mejor eso que perder la notificación entera.
            _logger.LogWarning(ex,
                "No se pudo adjuntar el PDF de \"{Title}\", se manda el link como texto en su lugar", offer.Title);
            await _bot.SendMessage(_chatId, $"📄 {offer.SourceSite}\n{offer.Title}\n{offer.Url}", cancellationToken: ct);
        }
    }

    private static bool IsPdfUrl(string url) => url.EndsWith(".pdf", StringComparison.OrdinalIgnoreCase);

    private static string BuildMessage(IReadOnlyList<JobOffer> offers)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"🔎 {offers.Count} nuevas ofertas que matchean tu perfil");
        sb.AppendLine();

        foreach (var group in offers.GroupBy(o => o.SourceSite))
        {
            sb.AppendLine($"— {group.Key} —");
            foreach (var offer in group)
            {
                var salary = string.IsNullOrWhiteSpace(offer.Salary) ? "" : $" | {offer.Salary}";
                sb.AppendLine($"• {offer.Title} — {offer.Company}{salary}");
                sb.AppendLine($"  {offer.Url}");
            }
            sb.AppendLine();
        }

        return sb.ToString();
    }

    private static IEnumerable<string> Chunk(string message, int maxLen)
    {
        for (int i = 0; i < message.Length; i += maxLen)
            yield return message.Substring(i, Math.Min(maxLen, message.Length - i));
    }
}