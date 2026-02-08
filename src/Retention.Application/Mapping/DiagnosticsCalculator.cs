using Retention.Application.Models;
using Retention.Domain.Models;

namespace Retention.Application.Mapping;

/// <summary>
/// Computes diagnostic counters from evaluation results.
/// </summary>
public interface IDiagnosticsCalculator
{
    RetentionDiagnostics Calculate(
        IReadOnlyList<ReleaseCandidate> candidates,
        int invalidExcludedCount,
        IReadOnlyList<KeptRelease> keptReleases);
}

/// <summary>
/// Default implementation matching original diagnostics computation.
/// </summary>
public sealed class DiagnosticsCalculator : IDiagnosticsCalculator
{
    public RetentionDiagnostics Calculate(
        IReadOnlyList<ReleaseCandidate> candidates,
        int invalidExcludedCount,
        IReadOnlyList<KeptRelease> keptReleases)
    {
        var groupsEvaluated = candidates
            .Select(c => (c.ProjectId, c.EnvironmentId))
            .Distinct()
            .Count();

        return new RetentionDiagnostics(
            GroupsEvaluated: groupsEvaluated,
            InvalidDeploymentsExcluded: invalidExcludedCount,
            TotalKeptReleases: keptReleases.Count);
    }
}
