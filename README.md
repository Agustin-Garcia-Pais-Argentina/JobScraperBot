JobScraperBot 🚀
Un bot automatizado construido en .NET diseñado para buscar, filtrar y notificar ofertas de trabajo personalizadas desde múltiples plataformas directamente a Telegram. Desarrollado por Agustín García, este proyecto implementa una arquitectura limpia y modular para procesar datos de APIs y extraer HTML de forma resiliente.

Arquitectura y Características
Extracción Multi-fuente: Integración nativa con endpoints JSON (RemoteOK, GetOnBoard, Remotive) y parseo del árbol DOM utilizando HtmlAgilityPack (WeRemoto).

Filtros Inteligentes: Pipeline de evaluación basado en expresiones regulares (\b) para descartar roles por seniority en el título y realizar match exacto del stack tecnológico.

Notificaciones Agrupadas: Sistema de alertas asíncronas hacia Telegram, dividiendo mensajes por portal y aplicando demoras estratégicas para respetar el Flood Control de la API.

Tolerancia a Fallos: Implementación de políticas de resiliencia con Polly para manejar caídas de red, timeouts y errores HTTP (404/429) de forma elegante.

Deduplicación de Datos: Registro local en JSON (seen-offers.json) que garantiza que una misma oferta nunca se envíe dos veces al canal.

Configuración del Perfil
El motor de búsqueda es agnóstico y 100% personalizable. Se controla mediante el archivo profile.json, donde se definen las preferencias exactas de búsqueda. Permite especificar ubicaciones objetivo (ej. Santa Fe, Remoto), lenguajes requeridos (C#, Vue, Python) y excluir palabras clave no deseadas para mantener la bandeja de entrada libre de spam.

Instalación y Uso
Para ejecutar el scraper localmente, asegúrate de tener el SDK de .NET instalado.

Clona este repositorio en tu entorno local.

Configura las credenciales de tu bot estableciendo las variables de entorno TELEGRAM_BOT_TOKEN y TELEGRAM_CHAT_ID.

Ejecuta el comando dotnet run --project src/JobScraperBot.App desde la raíz para iniciar el ciclo de extracción.

El proyecto también incluye un flujo de integración continua mediante GitHub Actions (scrape.yml) preparado para ejecutarse de manera periódica y autónoma en la nube.

Con esto, cualquiera que entre a tu repositorio va a entender al instante qué problema resuelve tu bot, cómo está construido por detrás y cómo levantarlo en su propia máquina.