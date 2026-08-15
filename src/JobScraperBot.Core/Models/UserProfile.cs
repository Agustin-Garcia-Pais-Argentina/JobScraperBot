namespace JobScraperBot.Core.Models;

/// <summary>
/// Configuración de negocio (perfil de búsqueda). Se carga desde profile.json,
/// editable sin recompilar. Ver setup-informacion-sistema.md para el detalle
/// de cada regla.
/// </summary>
public class UserProfile
{
    // Excluye SOLO si menciona explícitamente un nivel no deseado.
    // Ausencia de mención de seniority = NO se descarta.
    public List<string> SeniorityExcludeTerms { get; set; } = new();

    // OR entre perfiles: alcanza con que matchee UNO para que la oferta pase.
    public List<RoleProfile> RoleProfiles { get; set; } = new();

    public LocationProfile Location { get; set; } = new();
}

public class RoleProfile
{
    public string Name { get; set; } = "";

    // Vacío = sin exigencia de seniority propia (se apoya en el filtro global).
    public List<string> RequiredSeniorityTerms { get; set; } = new();

    // Cada sub-lista es un concepto obligatorio: AND entre grupos, OR dentro del grupo.
    // Vacío = sin filtro de área (usado por el perfil de Pasantías).
    public List<List<string>> RequiredKeywordGroups { get; set; } = new();

    public List<string> ExcludeKeywords { get; set; } = new();
}

public class LocationProfile
{
    public List<string> AcceptedOnsiteCities { get; set; } = new();

    // Ciudades/regiones que, si aparecen explícitamente, son otro lugar -> se descarta.
    // Todo lo que no caiga acá ni en AcceptedOnsiteCities se considera ambiguo -> se incluye.
    public List<string> KnownOtherCities { get; set; } = new();
}
