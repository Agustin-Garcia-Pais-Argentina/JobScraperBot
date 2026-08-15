using JobScraperBot.Core.Interfaces;
using JobScraperBot.Core.Models;

namespace JobScraperBot.Filters;

/// <summary>
/// Remoto: siempre pasa. Presencial/híbrido: pasa si es Santa Fe Capital,
/// o si la ubicación es ambigua (no menciona ninguna ciudad conocida).
/// Se descarta solo si menciona explícitamente OTRA ciudad conocida.
/// </summary>
public class LocationFilter : IJobFilter
{
    public string Name => "Location";

    public bool Matches(JobOffer offer, UserProfile profile)
    {
        if (offer.IsRemote) return true;

        var location = offer.Location ?? "";

        if (profile.Location.AcceptedOnsiteCities.Any(
                c => location.Contains(c, StringComparison.OrdinalIgnoreCase)))
            return true;

        var mentionsOtherCity = profile.Location.KnownOtherCities.Any(
            c => location.Contains(c, StringComparison.OrdinalIgnoreCase));

        // Ambiguo (no menciona ninguna ciudad reconocible) -> se incluye.
        return !mentionsOtherCity;
    }
}
