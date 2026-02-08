using Retention.Application.Mapping;
using Retention.Application.Models;

namespace Retention.Application.Evaluation.Steps;

/// <summary>
/// Step 7: Computes diagnostics and assembles the final RetentionResult.
/// </summary>
public sealed class FinalizeResultStep : IEvaluationStep
{
    private readonly IDiagnosticsCalculator _diagnosticsCalculator;

    public FinalizeResultStep(IDiagnosticsCalculator diagnosticsCalculator)
    {
        _diagnosticsCalculator = diagnosticsCalculator;
    }

    public void Execute(RetentionEvaluationContext context)
    {
        context.Diagnostics = _diagnosticsCalculator.Calculate(
            context.DomainCandidates,
            context.InvalidExcludedCount,
            context.KeptReleases);

        context.Result = new RetentionResult(
            context.KeptReleases,
            context.AllDecisionEntries,
            context.Diagnostics);
    }
}
