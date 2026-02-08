namespace Retention.Domain.Services;

/// <summary>
/// Default ranking strategy implementing ADR-0003 tie-breakers:
/// 1. LatestDeployedAt desc
/// 2. Release.Created desc
/// 3. Release.Id asc (ordinal)
/// </summary>
public sealed class DefaultRankingStrategy : IRetentionRankingStrategy
{
    public IReadOnlyList<RankedCandidate> Rank(IReadOnlyList<GroupEntry> entries)
    {
        var sorted = entries
            .OrderByDescending(x => x.LatestDeployedAt)
            .ThenByDescending(x => x.Created)
            .ThenBy(x => x.ReleaseId, StringComparer.Ordinal)
            .ToList();

        var result = new List<RankedCandidate>(sorted.Count);
        for (int i = 0; i < sorted.Count; i++)
        {
            var e = sorted[i];
            result.Add(new RankedCandidate(e.ReleaseId, e.Version, e.Created, e.LatestDeployedAt, Rank: i + 1));
        }

        return result;
    }
}
