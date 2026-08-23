

## 🎯 ¿Qué es JobScraperBot?

La búsqueda de empleo en tecnología suele estar llena de ruido: ofertas duplicadas, *spam* de palabras clave, o roles inflados en *seniority*. **JobScraperBot** resuelve esto automatizando la recolección de datos desde múltiples bolsas de trabajo (APIs y HTML), pasándolas por un pipeline de filtros de alta precisión, y entregando un resumen estructurado y limpio directamente en tu bolsillo a través de Telegram.

## 🏗️ Arquitectura y Diseño

El proyecto está diseñado siguiendo principios **SOLID** y una arquitectura en capas (*Layered Architecture*) para garantizar bajo acoplamiento y alta cohesión.

* **`JobScraperBot.Core`**: Contiene las interfaces (`IJobScraper`, `IJobFilter`), entidades del dominio (`JobOffer`, `UserProfile`) y contratos. Cero dependencias externas.
* **`JobScraperBot.Scrapers`**: Implementa el patrón *Strategy*. Cada portal (RemoteOK, GetOnBoard, WeRemoto) tiene su propio módulo de extracción, aislando los cambios si una plataforma actualiza su API o estructura DOM.
* **`JobScraperBot.Filters`**: Pipeline de evaluación secuencial. Utiliza validaciones con **Regex** (`\b`) para matcheo exacto de palabras clave y evaluación léxica de títulos (previniendo falsos positivos clásicos de *substrings*).
* **`JobScraperBot.Infrastructure`**: Capa de acceso a datos y comunicación externa. Maneja la persistencia de estado local (`seen-offers.json`) y el sistema de notificaciones asíncronas agrupadas hacia Telegram.
* **`JobScraperBot.Orchestration`**: El motor principal que ejecuta las tareas en paralelo, administra la concurrencia y maneja las políticas de reintentos.

## 🛠️ Stack Tecnológico y Librerías

* **Lenguaje & Framework:** C# 12 / .NET 8
* **Parsing & Extracción:** `System.Net.Http.Json` (APIs REST) y `HtmlAgilityPack` (Navegación DOM en SPAs/HTML).
* **Resiliencia y Tolerancia a Fallos:** `Polly` (Implementación de políticas de *Retry* y *Exponential Backoff* para manejar errores HTTP 429/404 y caídas de red).
* **Notificaciones:** `Telegram.Bot` API.
* **CI/CD:** GitHub Actions (Ejecución automatizada mediante CRON jobs en `scrape.yml`).

## ✨ Características Principales

* [x] **Scraping Híbrido:** Capaz de consumir contratos JSON:API o interceptar nodos HTML según la naturaleza de la bolsa de trabajo.
* [x] **Filtros Precisios:** Filtra ofertas analizando contextos y no solo palabras clave en su publicacion. (Ej: Descarta un rol si el título dice "Senior", pero lo acepta si la descripción dice "trabajarás con un Senior").
* [x] **Prevención de Spam (Flood Control):** El notificador agrupa las ofertas por origen y aplica delays estratégicos (`Task.Delay`) para no saturar la API de Telegram.
* [x] **Memoria:** Sistema de deduplicación que asegura que una misma oferta jamás se notifique dos veces.
* [x] **Camuflaje HTTP:** Inyección dinámica de cabeceras (*User-Agent*) para evitar bloqueos por parte de firewalls antibot.

## ⚙️ Configuración (`profile.json`)

El bot es 100% agnóstico y se configura editando un único archivo. Puedes definir tus locaciones deseadas, tu stack tecnológico, y palabras clave excluyentes para aniquilar el ruido:

```json
{
  "Keywords": ["C#", ".NET", "Vue", "Python", "SQL"],
  "ExcludedKeywords": ["WordPress", "PHP", "Call Center", "AI Supermarket"],
  "SeniorityExcludeTerms": ["senior", "ssr", "lead", "manager", "principal"],
  "TargetLocations": ["Santa Fe", "Argentina", "Remote"]
}

```

## 🚀 Instalación y Uso Local

1. **Cloná el repositorio:**
```bash
git clone https://github.com/Agustin-Garcia-Pais-Argentina/JobScraperBot.git
cd JobScraperBot

```


2. **Configurá tus variables de entorno:**
(Asegurate de haber creado un bot con BotFather en Telegram).
```powershell
$env:TELEGRAM_BOT_TOKEN="TU_TOKEN_AQUI"
$env:TELEGRAM_CHAT_ID="TU_CHAT_ID_AQUI"

```


3. **Ejecutá el orquestador:**
```bash
dotnet run --project src/JobScraperBot.App

```



---

*Desarrollado con ☕ y código limpio por Agustín García.*
