# JobScraperBot

Bot que scrapea ofertas de empleo de múltiples sitios, las filtra según un
perfil configurable, y envía un resumen por Telegram dos veces al día.
Corre 100% gratis en GitHub Actions.

Documentación de diseño completa:
- `arquitectura-bot-scraper-empleos.md` — arquitectura, patrón plug-in, stack.
- `setup-informacion-sistema.md` — detalle del `UserProfile` (perfil de búsqueda).

## Estructura

```
src/JobScraperBot.Core/            Modelos y contratos (sin dependencias externas)
src/JobScraperBot.Scrapers/        Un scraper "plugin" por sitio (implementa IJobScraper)
src/JobScraperBot.Filters/         Pipeline de filtrado (implementa IJobFilter)
src/JobScraperBot.Infrastructure/  Persistencia JSON, notificador de Telegram, resiliencia (Polly)
src/JobScraperBot.Orchestration/   Coordina el ciclo completo
src/JobScraperBot.App/             Entry point (consola) + profile.json
tests/                             Tests contra fixtures (no contra los sitios en vivo)
data/seen-offers.json              Estado persistente (lo commitea el propio workflow)
.github/workflows/scrape.yml       Scheduler (10hs/18hs) + hosting
```

## Setup local

```bash
dotnet restore JobScraperBot.sln
dotnet build JobScraperBot.sln
dotnet test JobScraperBot.sln

export TELEGRAM_BOT_TOKEN="..."
export TELEGRAM_CHAT_ID="..."
dotnet run --project src/JobScraperBot.App
```

## Setup de Telegram

1. Hablar con [@BotFather](https://t.me/BotFather) en Telegram, crear un bot con `/newbot` -> te da un `BOT_TOKEN`.
2. Mandarle un mensaje al bot recién creado (para "abrir" la conversación).
3. Consultar `https://api.telegram.org/bot<TOKEN>/getUpdates` para obtener tu `chat_id`.
4. Cargar `TELEGRAM_BOT_TOKEN` y `TELEGRAM_CHAT_ID` como **GitHub Secrets** del repo
   (Settings -> Secrets and variables -> Actions).

## Agregar un sitio nuevo

1. Copiar `src/JobScraperBot.Scrapers/ExampleHtmlScraper.cs` (o `RemotiveScraper.cs`
   si el sitio expone una API JSON), renombrar, ajustar `SiteName` y los selectores
   en `HtmlOfferMapper` (o la lógica de parseo propia).
2. Registrar el scraper en `src/JobScraperBot.App/Program.cs`:
   `services.AddSingleton<IJobScraper, TuScraperNuevo>();`
3. Nada más cambia: el Core, el pipeline de filtros, el notificador y el
   orquestador no se tocan.

## Correrlo manualmente en GitHub Actions

Actions -> "Scrape Job Offers" -> "Run workflow" (usa el trigger `workflow_dispatch`).
