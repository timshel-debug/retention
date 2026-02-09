using Retention.Domain.Services;

namespace Retention.Application.Evaluation.Steps;

/// <summary>
/// Step 4: Evaluates retention policy using the domain evaluator.
/// </summary>
public sealed class EvaluatePolicyStep : IEvaluationStep
{
    private readonly IRetentionPolicyEvaluator _evaluator;

    public EvaluatePolicyStep(IRetentionPolicyEvaluator evaluator)
    {
        _evaluator = evaluator;
    }

    public void Execute(RetentionEvaluationContext context)
    {
        if (context.ReferenceIndex is null)
            throw new InvalidOperationException("Pipeline invariant violation: ReferenceIndex must be set before EvaluatePolicyStep.");
        if (context.FilteredDeployments is null)
            throw new InvalidOperationException("Pipeline invariant violation: FilteredDeployments must be set by FilterInvalidDeploymentsStep before EvaluatePolicyStep.");

        context.DomainCandidates = _evaluator.Evaluate(
            context.ReferenceIndex.ReleasesById,
            context.FilteredDeployments.ValidDeployments,
            context.ReleasesToKeep);
    }
}
