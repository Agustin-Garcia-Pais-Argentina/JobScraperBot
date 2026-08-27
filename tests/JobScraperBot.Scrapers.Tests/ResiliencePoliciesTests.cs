using System.Net;
using JobScraperBot.Infrastructure.Resilience;
using Xunit;

namespace JobScraperBot.Scrapers.Tests;

public class ResiliencePoliciesTests
{
    [Fact]
    public async Task ExecuteWithRetryAsync_RetriesRetryableException_UsingRetryAfter()
    {
        var attempts = 0;

        var result = await ResiliencePolicies.ExecuteWithRetryAsync(
            () =>
            {
                attempts++;
                if (attempts < 2)
                    throw new RetryableScrapeException("rate limited", HttpStatusCode.TooManyRequests, TimeSpan.FromSeconds(5));

                return Task.FromResult("ok");
            },
            (ex, attempt, delay) =>
            {
                Assert.Equal(1, attempt);
                Assert.Equal(TimeSpan.FromSeconds(5), delay);
            });

        Assert.Equal("ok", result);
        Assert.Equal(2, attempts);
    }

    [Fact]
    public async Task ExecuteWithRetryAsync_DoesNotRetryPermanentExceptions()
    {
        var attempts = 0;

        await Assert.ThrowsAsync<PermanentScrapeException>(() => ResiliencePolicies.ExecuteWithRetryAsync<int>(
            () =>
            {
                attempts++;
                throw new PermanentScrapeException("not found", HttpStatusCode.NotFound);
            }));

        Assert.Equal(1, attempts);
    }

    [Fact]
    public void ParseRetryAfter_PrefersSeconds_WhenHeaderExists()
    {
        using var response = new HttpResponseMessage();
        response.Headers.TryAddWithoutValidation("Retry-After", "12");

        var delay = ResiliencePolicies.ParseRetryAfter(response.Headers);

        Assert.Equal(TimeSpan.FromSeconds(12), delay);
    }
}