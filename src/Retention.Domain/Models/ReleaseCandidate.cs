namespace Retention.Domain.Models;

/// <summary>
/// Represents a release candidate that has been evaluated for retention.
/// Contains all data needed by the application layer to build DTOs and decision logs.
/// </summary>
public sealed record ReleaseCandidate(
    string ProjectId,
    string EnvironmentId,
    string ReleaseId,
    string? Version,
    DateTimeOffset Created,
    DateTimeOffset LatestDeployedAt,
    int Rank,
    string ReasonCode);

/// <summary>
/// Stable reason codes for retention decisions.
/// </summary>
public static class ReasonCodes
{
    /// <summary>Release is kept because it is in the top N most recently deployed.</summary>
    public const string KeptTopN = "kept.top_n";
}
