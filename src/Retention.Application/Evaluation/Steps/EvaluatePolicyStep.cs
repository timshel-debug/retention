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
        context.DomainCandidates = _evaluator.Evaluate(
            context.ReferenceIndex!.ReleasesById,
            context.ValidDeployments,
            context.ReleasesToKeep);
    }
}
