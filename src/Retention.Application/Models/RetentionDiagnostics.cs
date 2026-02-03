namespace Retention.Application.Models;

/// <summary>
/// Diagnostic counters from retention evaluation.
/// </summary>
public sealed record RetentionDiagnostics(
    int GroupsEvaluated,
    int InvalidDeploymentsExcluded,
    int TotalKeptReleases);
