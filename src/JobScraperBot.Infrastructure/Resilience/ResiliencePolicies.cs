using System.Net;
using System.Net.Http.Headers;

namespace JobScraperBot.Infrastructure.Resilience;

/// <summary>
/// Política de reintento por scraper: solo reintenta errores transitorios y
/// respeta el header Retry-After cuando la API lo informa.
/// </summary>
public static class ResiliencePolicies
{
    public const int MaxRetryAttempts = 3;

    public static async Task<T> ExecuteWithRetryAsync<T>(
        Func<Task<T>> operation,
        Action<Exception, int, TimeSpan>? onRetry = null,
        CancellationToken cancellationToken = default)
    {
        Exception? lastException = null;

        for (var attempt = 1; attempt <= MaxRetryAttempts + 1; attempt++)
        {
            try
            {
                return await operation();
            }
            catch (Exception ex) when (IsRetryable(ex) && attempt <= MaxRetryAttempts)
            {
                lastException = ex;
                var delay = GetRetryDelay(ex, attempt);
                onRetry?.Invoke(ex, attempt, delay);

                if (cancellationToken.IsCancellationRequested)
                    throw;

                await Task.Delay(delay, cancellationToken);
            }
        }

        throw lastException ?? new InvalidOperationException("La operación de retry no pudo completar");
    }

    public static bool IsRetryable(Exception ex) =>
        ex is RetryableScrapeException or HttpRequestException or TimeoutException;

    public static TimeSpan GetRetryDelay(Exception ex, int attempt)
    {
        if (ex is RetryableScrapeException retryable && retryable.RetryAfter is not null)
        {
            var delay = retryable.RetryAfter.Value;
            return delay < TimeSpan.Zero ? TimeSpan.Zero : delay;
        }

        var fallback = TimeSpan.FromSeconds(Math.Pow(2, attempt));
        return fallback < TimeSpan.Zero ? TimeSpan.Zero : fallback;
    }

    public static TimeSpan? ParseRetryAfter(HttpResponseHeaders? headers)
    {
        if (headers is null || !headers.Contains("Retry-After"))
            return null;

        var value = headers.GetValues("Retry-After").FirstOrDefault();
        if (string.IsNullOrWhiteSpace(value))
            return null;

        if (int.TryParse(value, out var seconds))
            return TimeSpan.FromSeconds(seconds);

        if (DateTimeOffset.TryParse(value, out var retryAt))
        {
            var delay = retryAt - DateTimeOffset.UtcNow;
            return delay < TimeSpan.Zero ? TimeSpan.Zero : delay;
        }

        return null;
    }
}

public abstract class ScrapeFailureException : Exception
{
    public HttpStatusCode? StatusCode { get; }

    protected ScrapeFailureException(string message, HttpStatusCode? statusCode = null)
        : base(message)
    {
        StatusCode = statusCode;
    }
}

public sealed class RetryableScrapeException : ScrapeFailureException
{
    public TimeSpan? RetryAfter { get; }

    public RetryableScrapeException(string message, HttpStatusCode? statusCode = null, TimeSpan? retryAfter = null)
        : base(message, statusCode)
    {
        RetryAfter = retryAfter;
    }
}

public sealed class PermanentScrapeException : ScrapeFailureException
{
    public PermanentScrapeException(string message, HttpStatusCode? statusCode = null)
        : base(message, statusCode)
    {
    }
}
