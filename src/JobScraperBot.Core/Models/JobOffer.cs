namespace JobScraperBot.Core.Models;

public enum JobStatus { Active, Closed, Gone }

/// <summary>
/// Modelo común al que todo IJobScraper debe mapear sus resultados,
/// sin importar la estructura del sitio de origen.
/// </summary>
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
    JobStatus Status = JobStatus.Active
);
