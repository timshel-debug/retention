using Retention.Application;
using Retention.Application.Evaluation;
using Retention.Application.Evaluation.Steps;
using Retention.Application.Indexing;
using Retention.Application.Mapping;
using Retention.Application.Specifications;
using Retention.Application.Validation;
using Retention.Domain.Services;

namespace Retention.UnitTests.Helpers;

/// <summary>
/// Creates default engine and service instances for unit tests.
/// Mirrors the DI composition root but allows tests to construct without a container.
/// </summary>
internal static class TestEngineFactory
{
    /// <summary>
    /// Builds a <see cref="RetentionEvaluationEngine"/> with the default pipeline steps
    /// (no telemetry decorator, pure domain evaluator).
    /// </summary>
    public static RetentionEvaluationEngine CreateEngine(IRetentionPolicyEvaluator? evaluator = null)
    {
        evaluator ??= new RetentionPolicyEvaluator(
            new DefaultGroupRetentionEvaluator(new DefaultRankingStrategy(), new TopNSelectionStrategy()));

        var validationRules = ValidationRuleChainFactory.CreateDefaultChain();
        var indexBuilder = new ReferenceIndexBuilder();
        var spec = new DefaultDeploymentValiditySpecification();
        var assembler = new DecisionLogAssembler();
        var mapper = new KeptReleaseMapper();
        var diagnosticsCalc = new DiagnosticsCalculator();

        var steps = new IEvaluationStep[]
        {
            new ValidateInputsStep(validationRules),
            new BuildReferenceIndexStep(indexBuilder),
            new FilterInvalidDeploymentsStep(spec, assembler),
            new EvaluatePolicyStep(evaluator),
            new MapResultsStep(mapper),
            new BuildDecisionLogStep(assembler),
            new FinalizeResultStep(diagnosticsCalc),
        };

        return new RetentionEvaluationEngine(steps);
    }

    /// <summary>
    /// Builds an <see cref="EvaluateRetentionService"/> with the default pipeline
    /// (no telemetry decorator, pure domain evaluator).
    /// </summary>
    public static EvaluateRetentionService CreateService()
    {
        return new EvaluateRetentionService(CreateEngine());
    }
}
