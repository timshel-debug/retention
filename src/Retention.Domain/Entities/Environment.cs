namespace Retention.Domain.Entities;

/// <summary>
/// Represents a deployment target environment (e.g., Staging, Production).
/// </summary>
public sealed record Environment(string Id, string Name);
