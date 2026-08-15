namespace JobScraperBot.Core.Models;

/// <summary>Registro persistido en data/seen-offers.json.</summary>
public class SeenOfferRecord
{
    public string SourceSite { get; set; } = "";
    public string ExternalId { get; set; } = "";
    public DateTime FirstSeenAt { get; set; }
    public DateTime LastSeenAt { get; set; }
    public JobStatus Status { get; set; } = JobStatus.Active;
}
