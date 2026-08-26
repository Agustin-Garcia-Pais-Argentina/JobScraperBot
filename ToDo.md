## Criterio de priorización

El coeficiente combina la mejora que aporta cada modificación con la facilidad y velocidad de implementación:

```text
Coeficiente = (Mejora x 0.6) + (Factibilidad x 0.4)
```

Cada dimensión se puntúa de 1 a 5.

- `Mejora`: utilidad para el usuario, confiabilidad del bot y valor demostrable para el portfolio.
- `Factibilidad`: tiempo estimado, cantidad de archivos afectados y riesgo de romper comportamiento existente.

El coeficiente sirve para ordenar el trabajo, no para afirmar que una tarea con menor puntuación sea innecesaria.

## Tabla de prioridades

| Prioridad | Mejora | Mejora | Factibilidad | Coeficiente | Implementado|
|---|---|---:|---:|---:|---:|
| 1 | Evitar ejecuciones simultáneas en GitHub Actions | 5 | 5 | **5.0** | x |
| 2 | Crear tests para WeRemoto, Gtb y RemoteOk | 5 | 4 | **4.6** | x |
| 3 | IDs deterministas en lugar de GUID aleatorio | 5 | 4 | **4.6** | |
| 4 | Errores y reintentos más específicos | 5 | 3 | **4.2** | |
| 5 | Escritura atómica de `seen-offers.json` | 4 | 4 | **4.0** | |
| 6 | Mejorar detección de ubicaciones remotas | 3 | 5 | **3.8** | |
| 7 | Revisar estados `Active`, `Closed` y `Gone` | 5 | 2 | **3.8** | |
| 8 | Unificar AngleSharp/HtmlAgilityPack | 3 | 3 | **3.0** | |
| 9 | Migrar a `Host.CreateApplicationBuilder` e `IOptions` | 3 | 3 | **3.0** | |
| 10 | Seniority basado también en tags | 3 | 3 | **3.0** | |
| 11 | Reemplazar la estrategia de WeRemoto | 3 | 2 | **2.6** | |

## Fase 1: calidad visible y bajo riesgo

### 1. Crear tests para WeRemoto, Gtb y RemoteOk

- Objetivo: detectar cambios en cada fuente sin depender de que la API o el sitio estén disponibles durante los tests.
- Cómo hacerlo:
	- Guardar respuestas JSON o HTML representativas en `tests/JobScraperBot.Scrapers.Tests/Fixtures`.
	- Separar, cuando sea posible, el mapper de cada scraper de la llamada HTTP.
	- Probar casos normales, respuesta vacía, campos opcionales ausentes y estructura inválida.
	- Verificar especialmente `ExternalId`, `Url`, `Title`, `IsRemote` y `PublishedAt`.
- Mejora: tests rápidos, reproducibles y una red de seguridad para refactorizar.
- Alcance: principalmente tests y mappers; no debería modificar contratos del resto del sistema.
- Criterio de terminado: cada scraper tiene tests de mapeo y al menos un caso de respuesta vacía o incompleta.

### 2. IDs deterministas en lugar de GUID aleatorio

- Objetivo: que una misma oferta produzca siempre el mismo `ExternalId`.
- Cómo hacerlo:
	- Prioridad 1: usar el ID oficial del sitio.
	- Prioridad 2: usar una URL absoluta y normalizada.
	- Prioridad 3: generar un hash estable de sitio, título, empresa, ubicación y otros datos confiables.
	- Eliminar `Guid.NewGuid()` como fallback de deduplicación.
	- Centralizar la generación en un helper testeable.
- Mejora: evita notificaciones duplicadas y hace más confiable `seen-offers.json`.
- Impacto: algunas ofertas históricas podrían parecer nuevas una vez; se puede documentar la migración o reiniciar el JSON.
- Alcance: principalmente mappers y tests, aunque afecta el estado persistido.

### 3. Errores y reintentos más específicos

- Objetivo: distinguir errores transitorios de errores permanentes.
- Cómo hacerlo:
	- Reintentar timeouts, errores de red, `429` y ciertos `5xx`.
	- No reintentar JSON inválido, selectores rotos, `404` o errores de programación.
	- Usar `GetAsync` en scrapers que necesiten inspeccionar el status code.
	- Registrar sitio, intento y tipo de fallo.
- Mejora: menos esperas inútiles y logs más útiles.
- Impacto: puede cambiar la duración y el resultado de una corrida fallida, pero no el modelo de dominio.
- Alcance: resiliencia y scrapers; requiere tests de errores HTTP.

### 4. Evitar ejecuciones simultáneas en GitHub Actions  IMPLEMENTED

- Objetivo: impedir que dos corridas lean y escriban `data/seen-offers.json` al mismo tiempo.
- Cómo hacerlo:
	- Agregar `concurrency` al workflow con un grupo único.
	- Separar validación de Pull Requests de ejecución productiva.
	- Ejecutar el bot solo en `schedule` y `workflow_dispatch`, salvo necesidad concreta de hacerlo en cada `push`.
- Mejora: evita pérdida de estado, commits competidores y notificaciones duplicadas.
- Alcance: workflow y operación; no requiere cambiar C#.

## Fase 2: confiabilidad del sistema

### 5. Guardar `seen-offers.json` de forma atómica

- Objetivo: evitar que una interrupción deje el JSON incompleto.
- Cómo hacerlo:
	- Serializar primero a un archivo temporal en el mismo directorio.
	- Reemplazar el original solo cuando la escritura termine correctamente.
	- Manejar explícitamente `JsonException` al cargar un archivo corrupto.
- Mejora: protege la memoria de deduplicación ante cortes o fallos de escritura.
- Alcance: local a `JsonOfferRepository`; `IOfferRepository` puede mantenerse igual.

### 6. Mejorar detección de ubicaciones remotas

- Objetivo: reconocer `remoto`, `remote`, `work from home` y `fully distributed` sin hardcodear todas las reglas.
- Cómo hacerlo:
	- Agregar términos configurables al perfil, por ejemplo `RemoteTerms`.
	- Normalizar mayúsculas, espacios y separadores.
	- Evaluar los términos en `LocationFilter` y agregar tests.
- Mejora: menos ofertas remotas descartadas y reglas modificables sin recompilar.
- Impacto: algunas ofertas antes ambiguas podrían pasar como remotas.
- Alcance: `UserProfile`, `profile.json`, filtro y tests.

### 7. Revisar la semántica de estados `Active`, `Closed` y `Gone`

- Objetivo: que cada estado represente una situación comprobable.
- Cómo hacerlo:
	- Definir cuándo una fuente informa `Closed` y cuándo solo se puede inferir `Gone`.
	- Evitar marcar `Gone` después de una única ausencia si el scraping fue parcial.
	- Considerar un contador de ausencias consecutivas.
	- Agregar tests de reaparición, cierre explícito y desaparición temporal.
- Mejora: reduce falsos estados y errores provocados por fallos parciales.
- Impacto: modifica el modelo persistido y puede requerir migrar `seen-offers.json`.
- Alcance: cambio de dominio; conviene hacerlo después de estabilizar scrapers e IDs.

## Fase 3: refactor arquitectónico

### 8. Unificar AngleSharp/HtmlAgilityPack

- Objetivo: evitar dos formas de resolver el mismo problema de parsing.
- Cómo hacerlo:
	- Inventariar el uso actual.
	- Elegir una librería para código nuevo según selectores CSS, pruebas y necesidades reales.
	- Extraer helpers comunes solo para texto, atributos, URLs y normalización.
	- Migrar un scraper por vez conservando fixtures y tests.
- Mejora: menor carga cognitiva y menos duplicación.
- Riesgo: compartir librería no elimina las diferencias entre sitios; evitar un parser universal forzado.
- Alcance: varios scrapers y paquetes NuGet, sin necesidad de modificar `Core` u orquestación.

### 9. Migrar a `Host.CreateApplicationBuilder` e `IOptions`

- Objetivo: centralizar configuración, logging, DI y ciclo de vida usando el host estándar de .NET.
- Cómo hacerlo:
	- Registrar repositorio, notifier y orquestador en DI.
	- Agrupar token y chat ID en `TelegramOptions`.
	- Validar configuración al iniciar.
	- Mantener los contratos actuales.
- Mejora: menos construcción manual y una composición más preparada para crecer.
- Impacto: cambia principalmente `Program.cs`, no la lógica de negocio.
- Prioridad: media; no es necesario antes de resolver confiabilidad, IDs y tests.

## Fase 4: cambios dependientes del negocio

### 10. Seniority basado también en tags

- Objetivo: usar las señales de seniority que entrega cada fuente sin descartar una oferta solo porque menciona a un senior en la descripción.
- Cómo hacerlo:
	- Agregar tags a `JobOffer` si son parte del dominio, o conservarlos en cada DTO si solo sirven al scraper.
	- Dar mayor peso al título y los tags que a la descripción.
	- Reutilizar un matcher de palabras completas.
	- Probar `Senior`, `SSR`, `Lead`, `Manager`, guiones y menciones contextuales.
- Mejora: menos falsos positivos y reglas más coherentes.
- Impacto: agregar una propiedad a `JobOffer` afecta constructores, mappers, serialización y tests.
- Alcance: la lógica del filtro es local; el cambio del modelo común es transversal.

### 11. Revisar la estrategia de WeRemoto

- Objetivo: reemplazar la búsqueda indirecta por una fuente más directa y estable, si existe.
- Cómo hacerlo:
	- Identificar qué datos entrega hoy la estrategia actual.
	- Revisar API pública, RSS, HTML estático o URL de búsqueda directa.
	- Comparar cantidad de resultados, calidad de URLs y estabilidad.
	- Mantener la implementación detrás de `IJobScraper`.
- Mejora: menos dependencia de terceros, menor latencia y menos riesgo de bloqueos.
- Riesgo: la nueva fuente puede entregar menos resultados o tener límites propios.
- Alcance: local al scraper y su configuración; el contrato `IJobScraper` no debería cambiar.


Ideas:
1. Suma scoring de cv en base a oferta laboral con API de Gemini mas un reversionado del cv subido personalizadoa a la oferta especifica.
2. Sumar Computrabajo con scraper via email. (si es dificil de otra manera  )
3. Sumar mas fuentes como We Work Remotely, UniversoBit, Torre.ai, Wellfound, Otta, MercadoLibre/Eightfold (solo pq usa otra estrategia que sumaria a la complejidad del algoritmo).
