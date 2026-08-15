using JobScraperBot.Core.Models;

namespace JobScraperBot.Filters.Tests;

/// <summary>Réplica de profile.json para tests, sin depender de leer el archivo.</summary>
internal static class TestProfileFactory
{
    public static UserProfile Build() => new()
    {
        SeniorityExcludeTerms = new() { "senior", "ssr", "semi senior", "semi-senior", "sr.", "lead" },
        Location = new LocationProfile
        {
            AcceptedOnsiteCities = new() { "santa fe capital", "santa fe, santa fe", "ciudad de santa fe" },
            KnownOtherCities = new()
            {
                "rosario", "reconquista", "rafaela", "córdoba", "cordoba", "mendoza",
                "amba", "caba", "c.a.b.a", "ciudad autónoma de buenos aires", "buenos aires", "gba", "gran buenos aires"
            }
        },
        RoleProfiles = new()
        {
            new RoleProfile { Name = "Backend", RequiredKeywordGroups = new() { new() { "backend", "back-end", "server-side" } } },
            new RoleProfile { Name = "Full-stack", RequiredKeywordGroups = new() { new() { "full stack", "fullstack", "full-stack" } } },
            new RoleProfile { Name = "Data Analyst", RequiredKeywordGroups = new() { new() { "data analyst", "analista de datos" } } },
            new RoleProfile { Name = "Data Engineer", RequiredKeywordGroups = new() { new() { "data engineer", "ingeniero de datos" } } },
            new RoleProfile
            {
                Name = "Desarrollador",
                RequiredKeywordGroups = new()
                {
                    new() { "developer", "desarrollador", "programador", "engineer", "ingeniero" },
                    new() { "python", "java", "c#", ".net", "c++" }
                }
            },
            new RoleProfile
            {
                Name = "Pasantía / Trainee general",
                RequiredSeniorityTerms = new() { "pasantía", "pasantia", "trainee", "intern", "internship" },
                ExcludeKeywords = new()
                {
                    "soporte técnico", "soporte tecnico", "mesa de ayuda",
                    "help desk", "service desk", "atención al cliente", "call center"
                }
            }
        }
    };
}
