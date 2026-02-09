using Retention.Application.Evaluation.Steps;
using Retention.Application.Models;

namespace Retention.Application.Evaluation;

/// <summary>
/// Pure, deterministic evaluation engine. No telemetry, no side effects.
/// Composes the evaluation pipeline and returns a RetentionResult.
/// </summary>
public sealed class RetentionEvaluationEngine
{
    private readonly IReadOnlyList<IEvaluationStep> _steps;

    /// <summary>
    /// Creates an engine with the provided steps (injected via DI or tests).
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
