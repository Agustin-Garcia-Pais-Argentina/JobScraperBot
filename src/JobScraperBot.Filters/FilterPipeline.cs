using JobScraperBot.Core.Interfaces;
using JobScraperBot.Core.Models;

namespace JobScraperBot.Filters;

/// <summary>
/// Encadena todos los IJobFilter registrados (Chain of Responsibility).
/// Agregar un filtro nuevo = una clase nueva que implementa IJobFilter
/// y se registra en el DI container; el pipeline no cambia.
/// </summary>
public class FilterPipeline
{
    private readonly IReadOnlyList<IJobFilter> _filters;

    public FilterPipeline(IEnumerable<IJobFilter> filters) => _filters = filters.ToList();

    public IEnumerable<JobOffer> Apply(IEnumerable<JobOffer> offers, UserProfile profile)
        => offers.Where(o => _filters.All(f => f.Matches(o, profile)));
}
