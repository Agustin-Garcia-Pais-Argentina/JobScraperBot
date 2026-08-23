using JobScraperBot.Core.Interfaces;
using JobScraperBot.Core.Models;

namespace JobScraperBot.Filters;

public class CompanyExcludeFilter : IJobFilter
{
    public string Name => "CompanyExclude";

    public bool Matches(JobOffer offer, UserProfile profile)
    {
        if (profile.ExcludedCompanies is null || !profile.ExcludedCompanies.Any())
            return true;

        // Devuelve false (descarta la oferta) si la empresa de la oferta coincide con alguna de la lista negra
        return !profile.ExcludedCompanies.Any(
            badCompany => string.Equals(offer.Company, badCompany, StringComparison.OrdinalIgnoreCase));
    }
}