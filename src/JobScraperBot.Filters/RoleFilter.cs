using System.Text.RegularExpressions;
using JobScraperBot.Core.Interfaces;
using JobScraperBot.Core.Models;

namespace JobScraperBot.Filters;

public class RoleFilter : IJobFilter
{
    public string Name => "Role";

    public bool Matches(JobOffer offer, UserProfile profile)
    {
        var text = $"{offer.Title} {offer.RawDescription}";
        return profile.RoleProfiles.Any(role => RoleMatches(role, text));
    }

    private static bool RoleMatches(RoleProfile role, string text)
    {
        if (role.ExcludeKeywords.Any(term => ContainsExactWord(text, term)))
            return false;

        if (role.RequiredSeniorityTerms.Count > 0 &&
            !role.RequiredSeniorityTerms.Any(term => ContainsExactWord(text, term)))
            return false;

        return role.RequiredKeywordGroups.All(
            group => group.Any(term => ContainsExactWord(text, term)));
    }

    private static bool ContainsExactWord(string source, string term)
    {
        // \b asegura que sea una palabra completa (boundaries).
        return Regex.IsMatch(source, $@"\b{Regex.Escape(term)}\b", RegexOptions.IgnoreCase);
    }
}