using Retention.Application.Mapping;
using Retention.Application.Models;

namespace Retention.Application.Evaluation.Steps;

/// <summary>
/// Step 6: Builds the combined decision log (kept entries + diagnostics).
/// Preserves ordering: kept before diagnostic, then by ProjectId, EnvironmentId, Rank, ReleaseId.
/// </summary>
public sealed class BuildDecisionLogStep : IEvaluationStep
{
    private readonly IDecisionLogAssembler _assembler;

    public BuildDecisionLogStep(IDecisionLogAssembler assembler)
    {
        _assembler = assembler;
    }

    public void Execute(RetentionEvaluationContext context)
    {
        if (context.FilteredDeployments is null)
            throw new InvalidOperationException("Pipeline invariant violation: FilteredDeployments must be set by FilterInvalidDeploymentsStep before BuildDecisionLogStep.");

        var keptDecisions = context.DomainCandidates
            .Select(c => _assembler.BuildKeptEntry(c, context.ReleasesToKeep, context.CorrelationId))
            .ToList();

        context.KeptDecisionEntries = keptDecisions;

        context.AllDecisionEntries = keptDecisions
            .Concat(context.FilteredDeployments.DiagnosticEntries)
            .OrderBy(d => d.DecisionType == "kept" ? 0 : 1)
            .ThenBy(d => d.ProjectId, StringComparer.Ordinal)
            .ThenBy(d => d.EnvironmentId, StringComparer.Ordinal)
            .ThenBy(d => d.Rank)
            .ThenBy(d => d.ReleaseId, StringComparer.Ordinal)
            .ToList();
    }
}
