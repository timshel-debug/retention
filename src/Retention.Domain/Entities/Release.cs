namespace Retention.Domain.Entities;

/// <summary>
/// Represents a versioned snapshot of a project that can be deployed.
/// </summary>
public sealed record Release(
    string Id,
    string ProjectId,
    string? Version,
    DateTimeOffset Created);
