namespace Retention.Application.Models;

/// <summary>
/// Represents a release that should be kept based on retention policy evaluation.
/// </summary>
public sealed record KeptRelease(
    string ReleaseId,
    string ProjectId,
    string EnvironmentId,
    string? Version,
    DateTimeOffset Created,
    DateTimeOffset LatestDeployedAt,
    int Rank,
    string ReasonCode);
