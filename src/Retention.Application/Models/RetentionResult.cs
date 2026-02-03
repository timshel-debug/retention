namespace Retention.Application.Models;

/// <summary>
/// Result of retention policy evaluation containing kept releases and decision log.
/// </summary>
public sealed record RetentionResult(
    IReadOnlyList<KeptRelease> KeptReleases,
    IReadOnlyList<DecisionLogEntry> Decisions,
    RetentionDiagnostics Diagnostics);
