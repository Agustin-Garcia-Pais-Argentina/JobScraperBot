# Planificación Técnica: Bot de Scraping y Notificación de Ofertas Laborales

## 1. Resumen ejecutivo

Sistema que scrapea múltiples portales de empleo, normaliza y filtra las ofertas según un perfil configurable, y envía un resumen por Telegram dos veces al día. El requisito no negociable es **extensibilidad**: agregar un sitio nuevo debe implicar crear una clase nueva, no tocar el core. Segundo requisito: **costo $0** de infraestructura.

Stack propuesto: **.NET 8 (C#)**, con **AngleSharp/Playwright** para scraping, **GitHub Actions** como scheduler/hosting, **JSON versionado en el repo** para persistencia y deduplicación, y **Telegram Bot API** para el envío del resumen.

---

## 2. Arquitectura general (capas)

```
┌─────────────────────────────────────────────────────────────┐
│                  Scheduler (GitHub Actions cron)               │
│              Dispara el workflow a las 10:00 y 18:00 hs        │
└───────────────────────────┬────────────────────────────────┘
                            │
┌───────────────────────────▼────────────────────────────────┐
│                      Orchestrator (Job)                      │
│   Recorre todos los IJobScraper registrados y los ejecuta    │
│   en paralelo (con manejo de timeouts/errores por sitio)     │
└───────────────────────────┬────────────────────────────────┘
                            │
        ┌───────────────────┼───────────────────┐
        ▼                   ▼                   ▼
┌───────────────┐   ┌───────────────┐   ┌───────────────┐
│ Scraper Sitio A │   │ Scraper Sitio B │   │ Scraper Sitio N │  ← "Plugins"
│ (IJobScraper)   │   │ (IJobScraper)   │   │ (IJobScraper)   │
└───────┬───────┘   └───────┬───────┘   └───────┬───────┘
        └───────────────────┼───────────────────┘
                            ▼
                 ┌─────────────────────┐
                 │  Normalización       │  → convierte cada resultado
                 │  (Mapper por sitio)  │    a un modelo común JobOffer
                 └──────────┬──────────┘
                            ▼
                 ┌─────────────────────┐
                 │  Pipeline de filtros  │  → keywords, ubicación,
                 │  (Chain of Resp.)     │    salario, remoto, etc.
                 └──────────┬──────────┘
                            ▼
                 ┌─────────────────────┐
                 │  Deduplicación        │  → contra SQLite (evita
                 │  (Repository)         │    reenviar la misma oferta)
                 └──────────┬──────────┘
                            ▼
                 ┌─────────────────────┐
                 │  Generador de resumen │
                 └──────────┬──────────┘
                            ▼
                 ┌─────────────────────┐
                 │  Notificador WhatsApp │
                 │  (Twilio API)         │
                 └─────────────────────┘
```

**Por qué esta división en capas:**
- **Scheduler** desacoplado del negocio: si mañana cambiás de "2 veces al día" a "cada 4 horas", solo tocás config.
- **Orchestrator** no sabe nada de HTML ni de sitios específicos, solo sabe que existe una lista de `IJobScraper`.
- **Scrapers** son la única capa que conoce la estructura HTML de cada sitio → aislás el punto de falla.
- **Pipeline de filtros** separado del scraping: el filtro es sobre el modelo común, no sobre HTML, así podés reusar los mismos filtros para cualquier sitio nuevo.
- **Deduplicación** persistente evita spam de WhatsApp con ofertas ya vistas.

---

## 3. Abstracción del scraping: patrón Strategy + Factory (plugins)

La clave de la escalabilidad es que **cada sitio implementa la misma interfaz** y se registra solo, sin modificar el orquestador.

```csharp
// Modelo común de salida — todo scraper devuelve esto
public record JobOffer(
    string SourceSite,
    string ExternalId,
    string Title,
    string Company,
    string Location,
    bool IsRemote,
    string Url,
    DateTime PublishedAt,
    string? Salary,
    string RawDescription,
    JobStatus Status = JobStatus.Active // el scraper la setea en Closed si el sitio lo indica explícitamente
);

public enum JobStatus { Active, Closed, Gone } // Gone = desapareció del sitio (se detecta en Sección 5)

// Contrato que todo scraper debe cumplir
public interface IJobScraper
{
    string SiteName { get; }
    Task<IEnumerable<JobOffer>> ScrapeAsync(ScraperContext context, CancellationToken ct);
}

// Ejemplo de implementación concreta (un "plugin")
public class ComputrabajoScraper : IJobScraper
{
    public string SiteName => "Computrabajo";

    public async Task<IEnumerable<JobOffer>> ScrapeAsync(ScraperContext context, CancellationToken ct)
    {
        // lógica específica con AngleSharp/Playwright
        // parsea el HTML propio de este sitio
        // devuelve List<JobOffer> ya mapeado al modelo común
    }
}
```

**Registro automático (sin tocar el core al agregar un sitio):**

```csharp
// Program.cs — con DI de .NET, escaneo por reflexión/ensamblado
builder.Services.Scan(scan => scan
    .FromAssemblyOf<IJobScraper>()
    .AddClasses(c => c.AssignableTo<IJobScraper>())
    .AsImplementedInterfaces()
    .WithScopedLifetime());
```

Al agregar un sitio nuevo:
1. Creás una clase `NuevoSitioScraper : IJobScraper`.
2. La registrás (o se auto-registra por reflexión).
3. **No tocás Orchestrator, Pipeline, ni Notificador.**

**Resiliencia por scraper (Polly):** cada `IJobScraper` se ejecuta con `Polly` (retry + circuit breaker + timeout) para que si un sitio cambia su HTML o cae, **no tumbe la ejecución de los demás**. El orquestador corre todos con `Task.WhenAll` capturando excepciones individualmente (nunca un `throw` sin catch en el loop principal).

```csharp
foreach (var scraper in scrapers)
{
    tasks.Add(SafeScrapeAsync(scraper, ct)); // wrapper que loguea y devuelve [] en error
}
var results = await Task.WhenAll(tasks);
```

**Manejo de "el sitio cambió su HTML":** cada scraper loguea explícitamente cuando un selector no encuentra nodos esperados (ej. `logger.LogWarning("Selector .job-card no encontró resultados en {Site}")`), y esto se reporta como una alerta separada (podés mandarte un WhatsApp de "⚠️ Computrabajo puede haber cambiado de estructura, revisar selectors") en lugar de fallar silenciosamente.

---

## 4. Pipeline de filtrado (Chain of Responsibility)

```csharp
public interface IJobFilter
{
    bool Matches(JobOffer offer, UserProfile profile);
}

public class KeywordFilter : IJobFilter { /* título/descr. contiene C#, .NET, etc */ }
public class LocationFilter : IJobFilter { /* remoto o ciudad X */ }
public class SalaryFilter : IJobFilter { /* si el sitio expone salario */ }
public class SeniorityFilter : IJobFilter { /* excluye "trainee" si no aplica */ }

public class StalenessFilter : IJobFilter
{
    private readonly TimeSpan _maxAge = TimeSpan.FromDays(60);

    // Si el sitio no reporta PublishedAt confiable, se usa FirstSeenAt (ver Sección 5)
    public bool Matches(JobOffer offer, UserProfile profile)
        => (DateTime.UtcNow - offer.PublishedAt) <= _maxAge;
}

public class FilterPipeline
{
    private readonly IEnumerable<IJobFilter> _filters;

    public IEnumerable<JobOffer> Apply(IEnumerable<JobOffer> offers, UserProfile profile)
        => offers.Where(o => _filters.All(f => f.Matches(o, profile)));
}
```

`UserProfile` es config (JSON/appsettings o tabla en SQLite) editable sin recompilar: keywords, ubicaciones aceptadas, si aceptás híbrido, rango salarial mínimo, empresas a excluir (blacklist), etc.

Agregar un filtro nuevo = una clase nueva que implementa `IJobFilter`, mismo patrón plug-in que los scrapers.

---

## 5. Deduplicación, persistencia y manejo de ofertas vencidas

- **SQLite** (liviano, sin servidor, ideal para un bot personal; migrable a Postgres si esto crece a multiusuario).
- Tabla `SeenOffers` con índice único compuesto por sitio + id externo.
- Antes de notificar, se descartan ofertas ya en la tabla; las nuevas se insertan después de notificar con éxito (para no perder ofertas si falla el envío de WhatsApp).

Además de deduplicar, la tabla es responsable de descartar tres tipos de ofertas "muertas". Cada una tiene una señal de detección distinta:

```sql
CREATE TABLE SeenOffers (
    SourceSite   TEXT NOT NULL,
    ExternalId   TEXT NOT NULL,
    FirstSeenAt  DATETIME NOT NULL,
    LastSeenAt   DATETIME NOT NULL,
    Status       TEXT NOT NULL DEFAULT 'Active', -- Active, Closed, Gone
    PRIMARY KEY (SourceSite, ExternalId)
);
```

**a) Marcada explícitamente como "ocupada/cerrada" en el sitio.** Muchos portales muestran un badge tipo "Ya no se aceptan postulaciones". Esto lo detecta el **scraper del sitio** (solo él sabe leer ese HTML), seteando `Status = Closed` en el `JobOffer`. Si el sitio no expone esa info sin entrar al detalle (costoso de scrapear a granel), queda `Active` y depende de los otros dos mecanismos.

**b) Inactiva por antigüedad (hace 2+ meses).** Se resuelve con el `StalenessFilter` de la Sección 4, usando `PublishedAt`. Si el sitio no da una fecha confiable, se usa `FirstSeenAt` de esta tabla como fallback (la fecha en que el bot la vio por primera vez).

**c) Desapareció del sitio.** Este caso no se detecta mirando la oferta individual, sino por **ausencia** en la corrida actual: si algo estaba `Active` en la BDD y no volvió a aparecer en este scraping, se marca `Gone`.

```csharp
public async Task MarkGoneOffersAsync(string site, IEnumerable<string> idsSeenThisRun)
{
    // Solo se ejecuta si el scraper de este sitio corrió OK (sin excepción/timeout).
    // Si el sitio estuvo caído o el scraping falló, NO se marca nada como Gone:
    // "sitio caído" no es lo mismo que "oferta caída", y confundirlos borraría
    // de golpe todo el historial activo de ese sitio.
    await _db.SeenOffers
        .Where(o => o.SourceSite == site && o.Status == "Active" && !idsSeenThisRun.Contains(o.ExternalId))
        .ExecuteUpdateAsync(o => o.SetProperty(x => x.Status, "Gone"));
}
```

```csharp
if (scrapeResult.WasSuccessful && scrapeResult.Offers.Count > 0)
{
    await repository.MarkGoneOffersAsync(site, scrapeResult.Offers.Select(o => o.ExternalId));
}
```

**Pipeline completo resultante:**

```
Scrape → Normalizar → Filtrar (keywords/ubicación/salario/staleness)
       → Deduplicar contra Active/Closed conocidas en SeenOffers
       → Notificar solo lo nuevo y Active
       → Marcar Gone lo que no volvió a aparecer (solo si el scrape fue exitoso)
```

Esto asegura que nunca se notifique (ni se cuente en el resumen) una oferta `Closed` o `Gone`, incluso si por un scraping parcial/intermitente vuelve a aparecer momentáneamente con datos incompletos.

---

## 6. Ejecución programada y hosting: GitHub Actions (costo $0)

Con la prioridad de no pagar nada, **GitHub Actions reemplaza tanto al servidor como al scheduler**: no hay VPS, no hay contenedor corriendo 24/7, no hay Quartz.NET. La app pasa de ser un `Worker Service` persistente a una **consola simple** que corre una vez, hace el ciclo completo, y termina.

| Opción | Costo | Contras |
|---|---|---|
| **GitHub Actions (cron scheduled workflow)** | Gratis: 2000 min/mes en repos privados, ilimitado en públicos | El horario del cron puede tener algunos minutos de retraso en horas pico (no es garantía de segundo exacto) |
| VPS + Docker + Quartz.NET | ~$5-6/mes | Es justo lo que se quiere evitar |
| Azure Functions / AWS Lambda Timer | Free tier existe, pero con límites y tarjeta de crédito requerida | Cold starts, Playwright pesado para funciones serverless |

**Recomendación:** repo **privado** en GitHub (2000 minutos/mes gratis alcanzan de sobra: 2 corridas/día × ~2-3 min c/u ≈ 180 min/mes) con un workflow programado:

```yaml
# .github/workflows/scrape.yml
name: Scrape Job Offers

on:
  schedule:
    - cron: "0 13 * * *"  # 10:00 ART (UTC-3) → 13:00 UTC
    - cron: "0 21 * * *"  # 18:00 ART (UTC-3) → 21:00 UTC
  workflow_dispatch: {}   # permite correrlo manualmente para testear

permissions:
  contents: write   # necesario para commitear el archivo de estado

jobs:
  scrape:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v4

      - uses: actions/setup-dotnet@v4
        with:
          dotnet-version: "8.0.x"

      - name: Install Playwright browsers
        run: pwsh src/JobScraperBot.App/bin/Release/net8.0/playwright.ps1 install --with-deps chromium

      - name: Run bot
        run: dotnet run --project src/JobScraperBot.App --configuration Release
        env:
          TELEGRAM_BOT_TOKEN: ${{ secrets.TELEGRAM_BOT_TOKEN }}
          TELEGRAM_CHAT_ID: ${{ secrets.TELEGRAM_CHAT_ID }}

      - name: Persistir estado (ofertas vistas)
        uses: stefanzweifel/git-auto-commit-action@v5
        with:
          commit_message: "chore: update seen offers [skip ci]"
          file_pattern: "data/*.json"
```

**El problema a resolver:** los runners de GitHub Actions son efímeros (una VM nueva en cada corrida), así que la BDD de `SeenOffers` no persiste sola. Solución simple y gratuita: en vez de SQLite binario, el estado se guarda como **`data/seen-offers.json`** (texto plano, versionable, sin dependencias externas), y al final de cada corrida el propio workflow lo **commitea de vuelta al repo** con `git-auto-commit-action`. El siguiente `checkout` ya lo trae actualizado.

*(Si en algún momento el volumen de ofertas crece mucho y un JSON en git deja de ser prolijo, la migración natural es a una base gratis en la nube tipo **Turso** (SQLite serverless) o **Supabase/Neon** (Postgres free tier) — mismo `IOfferRepository`, solo cambia la implementación.)*

**Efecto colateral útil:** GitHub desactiva automáticamente los workflows programados en repos sin actividad por 60 días. Como este workflow commitea al repo en cada corrida, el repo nunca queda "inactivo" y el cron se mantiene activo solo.

**Riesgo a tener en cuenta:** algunos sitios pueden aplicar rate-limiting o bloqueos a IPs de datacenters compartidos (rango de IPs de GitHub-hosted runners). Si algún sitio empieza a devolver 403/captcha solo en Actions y no en local, ese scraper puntual necesitaría rotar user-agent/headers o, en el peor caso, correr ese sitio específico con menor frecuencia.

---

## 7. Notificación: Telegram vs Email (ambos gratis)

Se descarta WhatsApp por completo: incluso las opciones "oficiales" (Twilio, Meta Cloud API) tienen costo por mensaje o fricción de aprobación de plantillas, y las no oficiales rompen los ToS. Las dos alternativas gratuitas reales:

| Opción | Costo | Pros | Contras |
|---|---|---|---|
| **Telegram Bot API** (recomendada) | $0, sin límites relevantes para este uso | Setup en 2 minutos con [@BotFather](https://t.me/BotFather), API simple y estable, soporta Markdown/links/botones, notificación push instantánea en el celular | Necesitás tener Telegram instalado (no es una limitación real hoy) |
| **Email (SMTP)** | $0 (Gmail App Password, o free tier de SendGrid/Resend) | No depende de ninguna app nueva, fácil de archivar/buscar después | Menos inmediato que una notificación push, más fricción para leer en el momento |

**Recomendación:** **Telegram** como canal principal — es la opción más parecida a WhatsApp en experiencia (push instantáneo al celular) y 100% gratis sin letra chica. Dejamos el `INotifier` como interfaz para poder tener ambos simultáneamente si en algún momento querés el resumen también por mail.

```csharp
public interface INotifier
{
    Task SendSummaryAsync(IEnumerable<JobOffer> offers, CancellationToken ct);
}

public class TelegramNotifier : INotifier
{
    private readonly TelegramBotClient _bot; // Telegram.Bot (NuGet)
    private readonly string _chatId;

    public async Task SendSummaryAsync(IEnumerable<JobOffer> offers, CancellationToken ct)
    {
        var text = BuildSummaryText(offers); // Markdown: título, empresa, link, por sitio
        await _bot.SendTextMessageAsync(_chatId, text, parseMode: ParseMode.Markdown, cancellationToken: ct);
    }
}

public class EmailNotifier : INotifier
{
    // MailKit + Gmail App Password o SendGrid free tier (100 emails/día gratis)
    public async Task SendSummaryAsync(IEnumerable<JobOffer> offers, CancellationToken ct) { /* ... */ }
}
```

**Setup de Telegram (una sola vez):**
1. Hablás con `@BotFather`, creás un bot con `/newbot` → te da un `BOT_TOKEN`.
2. Le mandás un mensaje a tu bot recién creado (para "abrir" la conversación).
3. Consultás `https://api.telegram.org/bot<TOKEN>/getUpdates` para obtener tu `chat_id`.
4. Guardás `TELEGRAM_BOT_TOKEN` y `TELEGRAM_CHAT_ID` como **GitHub Secrets** del repo (nunca hardcodeados).

---

## 8. Justificación del stack (dado tu contexto)

- **C# / .NET 8**: ya es tu preferencia, tiene ecosistema maduro para todo lo que necesitás (DI nativa, Polly, HttpClientFactory), y correr como consola en un runner de GitHub Actions es trivial (`dotnet run` y listo). No hay razón técnica fuerte para cambiar de lenguaje acá; Python tendría scraping algo más simple (Scrapy/BeautifulSoup) pero perdés la tipificación fuerte que ayuda mucho cuando tenés 10+ mappers de HTML a mantener sin romper el modelo común.
- **AngleSharp** para scraping de HTML estático (rápido, sin overhead de browser).
- **Playwright for .NET** para los sitios que rendericen con JS (SPA) — mismo patrón `IJobScraper`, el `HttpClient` interno cambia pero la interfaz no.
- **JSON en el repo (`data/seen-offers.json`)** como persistencia: cero infraestructura, cero costo, diffs legibles en cada commit. Migrable a Turso/Postgres free-tier el día que el volumen lo justifique, sin tocar el resto del sistema (mismo `IOfferRepository`).
- **Polly** para resiliencia por sitio (retry/backoff/circuit breaker).
- **GitHub Actions** para scheduling y ejecución: reemplaza tanto al Worker Service/Quartz.NET como a cualquier servidor o contenedor — no hay nada corriendo 24/7 ni nada que pagar.
- **Telegram Bot API** para notificaciones: setup gratuito e inmediato, sin intermediarios ni costo por mensaje.

---

## 9. Estructura de proyecto inicial

```
JobScraperBot/
├── .github/
│   └── workflows/
│       └── scrape.yml                      # cron 10hs/18hs + commit de estado
│
├── src/
│   ├── JobScraperBot.Core/                 # Modelos y contratos (sin dependencias externas)
│   │   ├── Models/
│   │   │   ├── JobOffer.cs
│   │   │   └── UserProfile.cs
│   │   └── Interfaces/
│   │       ├── IJobScraper.cs
│   │       ├── IJobFilter.cs
│   │       ├── IOfferRepository.cs
│   │       └── INotifier.cs
│   │
│   ├── JobScraperBot.Scrapers/             # Plugins — un archivo por sitio
│   │   ├── ComputrabajoScraper.cs
│   │   ├── BumeranScraper.cs
│   │   ├── LinkedInScraper.cs
│   │   └── GetOnBoardScraper.cs
│   │
│   ├── JobScraperBot.Filters/
│   │   ├── KeywordFilter.cs
│   │   ├── LocationFilter.cs
│   │   └── SeniorityFilter.cs
│   │
│   ├── JobScraperBot.Infrastructure/
│   │   ├── Persistence/JsonOfferRepository.cs  # lee/escribe data/seen-offers.json
│   │   ├── Notifications/TelegramNotifier.cs
│   │   ├── Notifications/EmailNotifier.cs
│   │   └── Resilience/ (políticas Polly)
│   │
│   ├── JobScraperBot.Orchestration/
│   │   └── ScrapeOrchestrator.cs           # corre todos los IJobScraper y coordina el pipeline
│   │
│   └── JobScraperBot.App/                  # Entry point — consola simple, se ejecuta una vez y termina
│       ├── Program.cs
│       └── appsettings.json                # perfil de usuario (keywords, ubicación, etc.)
│
├── data/
│   └── seen-offers.json                    # estado persistente, se commitea solo tras cada run
│
├── tests/
│   ├── JobScraperBot.Scrapers.Tests/       # tests con HTML fixture guardado (evita golpear el sitio real)
│   └── JobScraperBot.Filters.Tests/
│
└── README.md
```

**Detalle clave para mantenibilidad:** en `tests`, cada scraper se testea contra un **archivo HTML guardado localmente** (fixture), no contra el sitio en vivo. Esto te permite detectar rápido si un cambio de HTML rompió el parser, sin depender de la disponibilidad del sitio ni arriesgar bloqueos por scraping excesivo durante desarrollo.

---

## 10. Próximos pasos sugeridos

1. Definir `UserProfile` (tu perfil real: keywords, ubicación, remoto sí/no, salario mínimo).
2. Implementar 1 scraper de referencia (el sitio que más te interese) y validar el pipeline completo end-to-end en local.
3. Crear el bot de Telegram con `@BotFather` y probar el envío.
4. Configurar el workflow de GitHub Actions y correrlo manualmente (`workflow_dispatch`) antes de dejarlo en cron.
5. Recién ahí, escalar agregando el resto de los sitios — el core ya no debería cambiar.

¿Querés que empecemos por el scaffolding real del proyecto (código C# de `Core` + un scraper de ejemplo funcionando), o preferís primero afinar el `UserProfile` y los criterios de filtrado?
