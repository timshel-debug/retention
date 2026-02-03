namespace Retention.Domain.Entities;

/// <summary>
/// Represents a deployment event - a release deployed to an environment at a specific time.
/// </summary>
public sealed record Deployment(
    string Id,
    string ReleaseId,
    string EnvironmentId,
    DateTimeOffset DeployedAt);
