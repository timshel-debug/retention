using BenchmarkDotNet.Attributes;
using Retention.Application.Evaluation;
using Retention.Application.Evaluation.Steps;
using Retention.Application.Indexing;
using Retention.Application.Mapping;
using Retention.Application.Specifications;
using Retention.Application.Validation;
using Retention.Domain.Services;

namespace Retention.Benchmarks.Benchmarks;

/// <summary>
/// Benchmarks for the retention evaluation pipeline (functional core only — no telemetry).
/// Satisfies PIPE-NFR-0001: pipeline refactor must not degrade performance by more than 10%.
/// </summary>
[MemoryDiagnoser]
[SimpleJob(warmupCount: 3, iterationCount: 10)]
public class RetentionEvaluationEngineBenchmarks
{
    private RetentionEvaluationEngine _engine = null!;
    private RetentionEvaluationInputs _smallInputs = null!;
    private RetentionEvaluationInputs _mediumInputs = null!;
    private RetentionEvaluationInputs _largeInputs = null!;

    [GlobalSetup]
    public void Setup()
    {
        // Use the pure evaluator (no telemetry decorator) to measure core logic only
        var evaluator = new RetentionPolicyEvaluator(
            new DefaultGroupRetentionEvaluator(new DefaultRankingStrategy(), new TopNSelectionStrategy()));

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

        _engine = new RetentionEvaluationEngine(steps);

        // Pre-build inputs so allocation is not measured
        _smallInputs = BenchmarkDataFactory.Small();
        _mediumInputs = BenchmarkDataFactory.Medium();
        _largeInputs = BenchmarkDataFactory.Large();
    }

    [Benchmark(Description = "Small (~50 deployments, 5 projects, 3 envs)")]
    public void Evaluate_Small()
    {
        _engine.Evaluate(_smallInputs);
    }

    [Benchmark(Description = "Medium (~600 deployments, 20 projects, 5 envs)")]
    public void Evaluate_Medium()
    {
        _engine.Evaluate(_mediumInputs);
    }

    [Benchmark(Baseline = true, Description = "Large (~6000 deployments, 50 projects, 10 envs)")]
    public void Evaluate_Large()
    {
        _engine.Evaluate(_largeInputs);
    }
}
