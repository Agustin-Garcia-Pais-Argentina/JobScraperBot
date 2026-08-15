using JobScraperBot.Core.Interfaces;
using JobScraperBot.Core.Models;

namespace JobScraperBot.Filters;

/// <summary>
/// Filtro global. Descarta SOLO si el título/descripción menciona
/// explícitamente un nivel no deseado (Senior, SSR, Lead...).
/// Si no dice nada de seniority, la oferta pasa (ante la duda, se incluye).
/// </summary>
public class SeniorityExcludeFilter : IJobFilter
{
    public string Name => "SeniorityExclude";

    public bool Matches(JobOffer offer, UserProfile profile)
    {
        var text = BuildSearchableText(offer);
        return !profile.SeniorityExcludeTerms.Any(
            term => text.Contains(term, StringComparison.OrdinalIgnoreCase));
    }

    private static string BuildSearchableText(JobOffer offer) => $"{offer.Title} {offer.RawDescription}";
}
