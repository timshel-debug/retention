using Retention.Application.Evaluation.Steps;
using Retention.Application.Indexing;
using Retention.Application.Mapping;
using Retention.Application.Models;
using Retention.Application.Specifications;
using Retention.Application.Validation;
using Retention.Domain.Services;

namespace Retention.Application.Evaluation;

/// <summary>
/// Pure, deterministic evaluation engine. No telemetry, no side effects.
/// Composes the evaluation pipeline and returns a RetentionResult.
/// </summary>
public sealed class RetentionEvaluationEngine
{
    private readonly IReadOnlyList<IEvaluationStep> _steps;

    /// <summary>
    /// Creates an engine with default components.
    /// </summary>
    public RetentionEvaluationEngine(IRetentionPolicyEvaluator evaluator)
    {
        var validationRules = ValidationRuleChainFactory.CreateDefaultChain();
        var indexBuilder = new ReferenceIndexBuilder();
        var spec = new DefaultDeploymentValiditySpecification();
        var assembler = new DecisionLogAssembler();
        var mapper = new KeptReleaseMapper();
        var diagnosticsCalc = new DiagnosticsCalculator();

        _steps = new IEvaluationStep[]
        {
            new ValidateInputsStep(validationRules),
            new BuildReferenceIndexStep(indexBuilder),
            new FilterInvalidDeploymentsStep(spec, assembler),
            new EvaluatePolicyStep(evaluator),
            new MapResultsStep(mapper),
            new BuildDecisionLogStep(assembler),
            new FinalizeResultStep(diagnosticsCalc),
        };
    }

    /// <summary>
    /// Creates an engine with custom steps (for testing or extension).
    /// </summary>
    public RetentionEvaluationEngine(IReadOnlyList<IEvaluationStep> steps)
    {
        _steps = steps;
    }

    /// <summary>
    /// Evaluates retention and returns the result. Pure and deterministic.
    /// </summary>
    public RetentionResult? Evaluate(RetentionEvaluationInputs inputs)
    {
        var context = new RetentionEvaluationContext
        {
            Projects = inputs.Projects,
            Environments = inputs.Environments,
            Releases = inputs.Releases,
            Deployments = inputs.Deployments,
            ReleasesToKeep = inputs.ReleasesToKeep,
            CorrelationId = inputs.CorrelationId,
        };

        foreach (var step in _steps)
        {
            step.Execute(context);
        }

        return context.Result;
    }
}
