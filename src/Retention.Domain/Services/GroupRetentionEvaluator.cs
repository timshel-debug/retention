using Retention.Domain.Models;

namespace Retention.Domain.Services;

/// <summary>
/// Evaluates retention for a single (ProjectId, EnvironmentId) group.
/// Template Method pattern: provides hook points for instrumentation.
/// </summary>
public interface IGroupRetentionEvaluator
{
    /// <summary>
    /// Evaluates a single group and returns kept candidates.
    /// </summary>
    IReadOnlyList<ReleaseCandidate> EvaluateGroup(
        string projectId,
        string environmentId,
        IReadOnlyList<GroupEntry> entries,
        int releasesToKeep);
}

/// <summary>
/// Default group evaluator using ranking + selection strategies.
/// </summary>
public class DefaultGroupRetentionEvaluator : IGroupRetentionEvaluator
{
    private readonly IRetentionRankingStrategy _rankingStrategy;
    private readonly IRetentionSelectionStrategy _selectionStrategy;

    public DefaultGroupRetentionEvaluator(
        IRetentionRankingStrategy rankingStrategy,
        IRetentionSelectionStrategy selectionStrategy)
    {
        _rankingStrategy = rankingStrategy;
        _selectionStrategy = selectionStrategy;
    }

    public virtual IReadOnlyList<ReleaseCandidate> EvaluateGroup(
        string projectId,
        string environmentId,
        IReadOnlyList<GroupEntry> entries,
        int releasesToKeep)
    {
        // Step 1: Rank candidates
        var ranked = _rankingStrategy.Rank(entries);

        // Step 2: Select top N
        var selected = _selectionStrategy.Select(ranked, releasesToKeep);

        // Step 3: Map to domain candidates
        var result = new List<ReleaseCandidate>(selected.Count);
        foreach (var candidate in selected)
        {
            result.Add(new ReleaseCandidate(
                ProjectId: projectId,
                EnvironmentId: environmentId,
                ReleaseId: candidate.ReleaseId,
                Version: candidate.Version,
                Created: candidate.Created,
                LatestDeployedAt: candidate.LatestDeployedAt,
                Rank: candidate.Rank,
                ReasonCode: ReasonCodes.KeptTopN));
        }

        return result;
    }
}
