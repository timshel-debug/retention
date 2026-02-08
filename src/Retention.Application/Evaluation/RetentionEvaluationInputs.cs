using Retention.Application.Models;
using Retention.Domain.Entities;
using Environment = Retention.Domain.Entities.Environment;

namespace Retention.Application.Evaluation;

/// <summary>
/// Immutable input bundle for the evaluation engine.
/// </summary>
public sealed record RetentionEvaluationInputs(
    IReadOnlyList<Project> Projects,
    IReadOnlyList<Environment> Environments,
    IReadOnlyList<Release> Releases,
    IReadOnlyList<Deployment> Deployments,
    int ReleasesToKeep,
    string? CorrelationId);
