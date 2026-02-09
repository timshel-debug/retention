using Retention.Application;
using Retention.Application.Evaluation;
using Retention.Application.Evaluation.Steps;
using Retention.Application.Indexing;
using Retention.Application.Mapping;
using Retention.Application.Specifications;
using Retention.Application.Validation;
using Retention.Domain.Services;

namespace Retention.IntegrationTests.Helpers;

/// <summary>
/// Creates default engine and service instances for integration tests.
/// Mirrors the DI composition root but allows direct construction without a container.
/// </summary>
internal static class TestEngineFactory
{
    public static RetentionEvaluationEngine CreateEngine(IRetentionPolicyEvaluator? evaluator = null)
    {
        evaluator ??= new RetentionPolicyEvaluator(
            new TelemetryGroupRetentionEvaluator(
                new DefaultGroupRetentionEvaluator(new DefaultRankingStrategy(), new TopNSelectionStrategy())));

        var validationRules = ValidationRuleChainFactory.CreateDefaultChain();
        var assembler = new DecisionLogAssembler();

        var steps = new IEvaluationStep[]
        {
            new ValidateInputsStep(validationRules),
            new BuildReferenceIndexStep(new ReferenceIndexBuilder()),
            new FilterInvalidDeploymentsStep(new DefaultDeploymentValiditySpecification(), assembler),
            new EvaluatePolicyStep(evaluator),
            new MapResultsStep(new KeptReleaseMapper()),
            new BuildDecisionLogStep(assembler),
            new FinalizeResultStep(new DiagnosticsCalculator()),
        };

        return new RetentionEvaluationEngine(steps);
    }

    public static EvaluateRetentionService CreateService()
    {
        return new EvaluateRetentionService(CreateEngine());
    }
}
