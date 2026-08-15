using Polly;
using Polly.Retry;

namespace JobScraperBot.Infrastructure.Resilience;

/// <summary>
/// Política de reintento por scraper: si un sitio falla (timeout, HTML
/// distinto, error de red), reintenta con backoff exponencial antes de
/// darlo por perdido para esa corrida. Así un sitio caído no arrastra a
/// los demás.
/// </summary>
public static class ResiliencePolicies
{
    public static AsyncRetryPolicy CreateScraperRetryPolicy(Action<Exception, int> onRetry) =>
        Policy
            .Handle<Exception>()
            .WaitAndRetryAsync(
                retryCount: 3,
                sleepDurationProvider: attempt => TimeSpan.FromSeconds(Math.Pow(2, attempt)),
                onRetry: (ex, _, attempt, _) => onRetry(ex, attempt));
}
