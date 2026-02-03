namespace Retention.Domain.Entities;

/// <summary>
/// Represents a deployable project/application in DevOps Deploy.
/// </summary>
public sealed record Project(string Id, string Name);
