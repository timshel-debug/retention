using BenchmarkDotNet.Attributes;
using Retention.Application.Evaluation;
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
        var evaluator = new RetentionPolicyEvaluator();
        _engine = new RetentionEvaluationEngine(evaluator);

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
