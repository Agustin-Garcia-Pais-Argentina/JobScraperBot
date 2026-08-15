using System.Text.Json;
using JobScraperBot.Core.Interfaces;
using JobScraperBot.Core.Models;
using Microsoft.Extensions.Logging;

namespace JobScraperBot.Infrastructure.Persistence;

/// <summary>
/// Persistencia "pobre pero gratis": el estado vive en data/seen-offers.json,
/// que GitHub Actions commitea de vuelta al repo al final de cada corrida
/// (ver .github/workflows/scrape.yml). Sin servidor, sin costo.
/// </summary>
public class JsonOfferRepository : IOfferRepository
{
    private readonly string _filePath;
    private readonly ILogger<JsonOfferRepository> _logger;
    private Dictionary<string, SeenOfferRecord> _records = new();

    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };

    public JsonOfferRepository(string filePath, ILogger<JsonOfferRepository> logger)
    {
        _filePath = filePath;
        _logger = logger;
        Load();
    }

    private void Load()
    {
        if (!File.Exists(_filePath))
        {
            _logger.LogInformation("No existe {Path} todavía, se arranca con estado vacío", _filePath);
            _records = new();
            return;
        }

        var json = File.ReadAllText(_filePath);
        var list = JsonSerializer.Deserialize<List<SeenOfferRecord>>(json) ?? new();
        _records = list.ToDictionary(Key);
    }

    private static string Key(SeenOfferRecord r) => $"{r.SourceSite}::{r.ExternalId}";
    private static string Key(JobOffer o) => $"{o.SourceSite}::{o.ExternalId}";

    public Task<HashSet<string>> GetKnownActiveOrClosedIdsAsync(string site, CancellationToken ct)
    {
        var ids = _records.Values
            .Where(r => r.SourceSite == site && r.Status != JobStatus.Gone)
            .Select(r => r.ExternalId)
            .ToHashSet();

        return Task.FromResult(ids);
    }

    public Task MarkSeenAsync(IEnumerable<JobOffer> offers, CancellationToken ct)
    {
        var now = DateTime.UtcNow;
        foreach (var offer in offers)
        {
            var key = Key(offer);
            if (_records.TryGetValue(key, out var existing))
            {
                existing.LastSeenAt = now;
                existing.Status = offer.Status;
            }
            else
            {
                _records[key] = new SeenOfferRecord
                {
                    SourceSite = offer.SourceSite,
                    ExternalId = offer.ExternalId,
                    FirstSeenAt = now,
                    LastSeenAt = now,
                    Status = offer.Status
                };
            }
        }
        return Task.CompletedTask;
    }

    public Task MarkGoneOffersAsync(string site, IEnumerable<string> idsSeenThisRun, CancellationToken ct)
    {
        var seenSet = idsSeenThisRun.ToHashSet();
        foreach (var record in _records.Values)
        {
            if (record.SourceSite == site && record.Status == JobStatus.Active && !seenSet.Contains(record.ExternalId))
            {
                record.Status = JobStatus.Gone;
            }
        }
        return Task.CompletedTask;
    }

    public async Task SaveAsync(CancellationToken ct)
    {
        var dir = Path.GetDirectoryName(_filePath);
        if (!string.IsNullOrEmpty(dir)) Directory.CreateDirectory(dir);

        var json = JsonSerializer.Serialize(_records.Values.OrderBy(r => r.SourceSite).ThenBy(r => r.ExternalId).ToList(), JsonOptions);
        await File.WriteAllTextAsync(_filePath, json, ct);
    }
}
