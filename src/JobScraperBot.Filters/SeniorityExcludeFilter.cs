using JobScraperBot.Core.Interfaces;
using JobScraperBot.Core.Models;

namespace JobScraperBot.Filters;

/// <summary>
/// Filtro global. Descarta SOLO si el TÍTULO menciona
/// explícitamente un nivel no deseado (Senior, SSR, Lead...).
/// Evita escanear la descripción para no descartar falsos positivos 
/// (ej: "reportarás a un desarrollador Senior").
/// </summary>
public class SeniorityExcludeFilter : IJobFilter
{
    public string Name => "SeniorityExclude";

    public bool Matches(JobOffer offer, UserProfile profile)
    {
        // Check de seguridad por si alguna oferta viene sin título
        if (string.IsNullOrWhiteSpace(offer.Title))
            return true;

        // Escaneamos puramente el título
        return !profile.SeniorityExcludeTerms.Any(
            term => offer.Title.Contains(term, StringComparison.OrdinalIgnoreCase));
    }
}