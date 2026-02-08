using Retention.Application.Models;
using Retention.Domain.Entities;

namespace Retention.Application.Evaluation;

/// <summary>
/// Result of filtering deployments for invalid references.
/// Contains both valid deployments and diagnostic entries for excluded ones.
/// </summary>
public sealed class FilteredDeploymentsResult
{
    public IReadOnlyList<Deployment> ValidDeployments { get; }
    public IReadOnlyList<DecisionLogEntry> DiagnosticEntries { get; }
    public int InvalidExcludedCount { get; }

    public FilteredDeploymentsResult(
        IReadOnlyList<Deployment> validDeployments,
        IReadOnlyList<DecisionLogEntry> diagnosticEntries,
        int invalidExcludedCount)
    {
        ValidDeployments = validDeployments;
        DiagnosticEntries = diagnosticEntries;
        InvalidExcludedCount = invalidExcludedCount;
    }
}
