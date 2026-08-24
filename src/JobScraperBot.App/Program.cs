using System.Text.Json;
using JobScraperBot.Core.Interfaces;
using JobScraperBot.Core.Models;
using JobScraperBot.Filters;
using JobScraperBot.Infrastructure.Notifications;
using JobScraperBot.Infrastructure.Persistence;
using JobScraperBot.Orchestration;
using JobScraperBot.Scrapers;
using JobScraperBot.Scrapers.Utn;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

var services = new ServiceCollection();

services.AddLogging(b => b.AddSimpleConsole(o =>
{
    o.SingleLine = true;
    o.TimestampFormat = "HH:mm:ss ";
}));

// --- Scrapers registrados (plugins) ---
// Para agregar un sitio nuevo: crear la clase IJobScraper en JobScraperBot.Scrapers
// y agregar una línea acá. Nada más del sistema necesita cambiar.
services.AddHttpClient<RemotiveScraper>();
services.AddSingleton<IJobScraper, RemotiveScraper>();

services.AddHttpClient<UtnBusquedasLaboralesScraper>();
services.AddSingleton<IJobScraper, UtnBusquedasLaboralesScraper>();

services.AddHttpClient<UtnPasantiasScraper>();
services.AddSingleton<IJobScraper, UtnPasantiasScraper>();

services.AddHttpClient<IJobScraper, RemoteOkScraper>();
services.AddSingleton<IJobScraper, RemoteOkScraper>();

services.AddHttpClient<IJobScraper, WeRemotoScraper>();
services.AddSingleton<IJobScraper, WeRemotoScraper>();

services.AddHttpClient<IJobScraper, GetOnBoardScraper>();
services.AddSingleton<IJobScraper, GetOnBoardScraper>();


// --- Filtros del pipeline ---
services.AddSingleton<IJobFilter, CompanyExcludeFilter>();
services.AddSingleton<IJobFilter, RoleFilter>();
services.AddSingleton<IJobFilter, LocationFilter>();
services.AddSingleton<IJobFilter, SeniorityExcludeFilter>();
services.AddSingleton<FilterPipeline>();

using var provider = services.BuildServiceProvider();

var repoRoot = FindRepoRoot(AppContext.BaseDirectory);
var dataPath = Path.Combine(repoRoot, "data", "seen-offers.json");
var profilePath = Path.Combine(AppContext.BaseDirectory, "profile.json");

var profile = JsonSerializer.Deserialize<UserProfile>(
    File.ReadAllText(profilePath),
    new JsonSerializerOptions { PropertyNameCaseInsensitive = true })
    ?? throw new InvalidOperationException("No se pudo leer profile.json");

var repository = new JsonOfferRepository(dataPath, provider.GetRequiredService<ILogger<JsonOfferRepository>>());

var botToken = Environment.GetEnvironmentVariable("TELEGRAM_BOT_TOKEN")
    ?? throw new InvalidOperationException("Falta la variable de entorno TELEGRAM_BOT_TOKEN");
var chatId = Environment.GetEnvironmentVariable("TELEGRAM_CHAT_ID")
    ?? throw new InvalidOperationException("Falta la variable de entorno TELEGRAM_CHAT_ID");

var notifier = new TelegramNotifier(
    botToken, chatId,
    provider.GetRequiredService<IHttpClientFactory>().CreateClient(),
    provider.GetRequiredService<ILogger<TelegramNotifier>>());

var orchestrator = new ScrapeOrchestrator(
    provider.GetServices<IJobScraper>(),
    provider.GetRequiredService<FilterPipeline>(),
    repository,
    notifier,
    provider.GetRequiredService<ILogger<ScrapeOrchestrator>>());

await orchestrator.RunFullCycleAsync(profile, CancellationToken.None);

static string FindRepoRoot(string startDir)
{
    var dir = new DirectoryInfo(startDir);
    while (dir is not null && dir.GetFiles("*.sln").Length == 0)
        dir = dir.Parent;

    return dir?.FullName ?? startDir;
}