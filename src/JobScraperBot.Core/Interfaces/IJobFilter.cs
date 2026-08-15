using JobScraperBot.Core.Models;

namespace JobScraperBot.Core.Interfaces;

public interface IJobFilter
{
    string Name { get; }
    bool Matches(JobOffer offer, UserProfile profile);
}
