using JobScraperBot.Core.Interfaces;
using JobScraperBot.Core.Models;

namespace JobScraperBot.Filters;

/// <summary>
/// Pasa si la oferta matchea AL MENOS UNO de los RoleProfiles configurados
/// (Backend, Full-stack, Data Analyst, Data Engineer, Desarrollador, Pasantías...).
/// </summary>
public class RoleFilter : IJobFilter
{
    public string Name => "Role";

    public bool Matches(JobOffer offer, UserProfile profile)
    {
        var text = $"{offer.Title} {offer.RawDescription}".ToLowerInvariant();
        return profile.RoleProfiles.Any(role => RoleMatches(role, text));
    }

    private static bool RoleMatches(RoleProfile role, string text)
    {
        if (role.ExcludeKeywords.Any(term => text.Contains(term.ToLowerInvariant())))
            return false;

        if (role.RequiredSeniorityTerms.Count > 0 &&
            !role.RequiredSeniorityTerms.Any(term => text.Contains(term.ToLowerInvariant())))
            return false;

        // AND entre grupos, OR dentro de cada grupo. Un RoleProfile sin grupos
        // (ej. Pasantía general) no exige ningún término de área.
        return role.RequiredKeywordGroups.All(
            group => group.Any(term => text.Contains(term.ToLowerInvariant())));
    }
}
