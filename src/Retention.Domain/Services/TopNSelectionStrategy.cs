namespace Retention.Domain.Services;

/// <summary>
/// Selection strategy that keeps the first N candidates from ranked list.
/// </summary>
public sealed class TopNSelectionStrategy : IRetentionSelectionStrategy
{
    public IReadOnlyList<RankedCandidate> Select(IReadOnlyList<RankedCandidate> ranked, int releasesToKeep)
    {
        return ranked.Take(releasesToKeep).ToList();
    }
}
