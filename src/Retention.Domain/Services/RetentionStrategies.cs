using Retention.Domain.Models;

namespace Retention.Domain.Services;

/// <summary>
/// Represents a deployment entry within a (ProjectId, EnvironmentId) group.
/// Used as input to ranking strategies.
/// </summary>
public sealed record GroupEntry(
    string ReleaseId,
    string? Version,
    DateTimeOffset Created,
    DateTimeOffset LatestDeployedAt);

/// <summary>
/// A ranked candidate after strategy ranking+selection.
/// </summary>
public sealed record RankedCandidate(
    string ReleaseId,
    string? Version,
    DateTimeOffset Created,
    DateTimeOffset LatestDeployedAt,
    int Rank);

/// <summary>
/// Strategy for ranking eligible releases within a group.
/// </summary>
public interface IRetentionRankingStrategy
{
    IReadOnlyList<RankedCandidate> Rank(IReadOnlyList<GroupEntry> entries);
}

/// <summary>
/// Strategy for selecting which ranked candidates to keep.
/// </summary>
public interface IRetentionSelectionStrategy
{
    IReadOnlyList<RankedCandidate> Select(IReadOnlyList<RankedCandidate> ranked, int releasesToKeep);
}
