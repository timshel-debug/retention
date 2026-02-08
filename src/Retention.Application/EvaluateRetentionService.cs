using System.Diagnostics;
using Retention.Application.Errors;
using Retention.Application.Evaluation;
using Retention.Application.Models;
using Retention.Application.Observability;
using Retention.Domain.Entities;
using Retention.Domain.Services;

namespace Retention.Application;

public interface IEvaluateRetentionService
{
	RetentionResult EvaluateRetention(
		IReadOnlyList<Project>? projects,
		IReadOnlyList<Domain.Entities.Environment>? environments,
		IReadOnlyList<Release>? releases,
		IReadOnlyList<Deployment>? deployments,
		int releasesToKeep,
		string? correlationId = null);
}

/// <summary>
/// Application service that orchestrates retention evaluation (imperative shell).
/// Handles null normalization, telemetry Activity/Stopwatch, and exception recording.
/// Delegates pure evaluation to <see cref="RetentionEvaluationEngine"/>.
/// </summary>
public sealed class EvaluateRetentionService : IEvaluateRetentionService
{
    private readonly RetentionEvaluationEngine _engine;

    public EvaluateRetentionService()
        : this(new RetentionPolicyEvaluator(
            new TelemetryGroupRetentionEvaluator(new DefaultGroupRetentionEvaluator())))
    {
    }

    public EvaluateRetentionService(IRetentionPolicyEvaluator evaluator)
    {
        ArgumentNullException.ThrowIfNull(evaluator);
        _engine = new RetentionEvaluationEngine(evaluator);
    }

    /// <summary>
    /// Evaluates retention policy for all project/environment combinations in the input.
    /// </summary>
    public RetentionResult EvaluateRetention(
        IReadOnlyList<Project>? projects,
        IReadOnlyList<Domain.Entities.Environment>? environments,
        IReadOnlyList<Release>? releases,
        IReadOnlyList<Deployment>? deployments,
        int releasesToKeep,
        string? correlationId = null)
    {
        // REQ-0009: Validate n >= 0 (before null coalescing, matching original behavior)
        if (releasesToKeep < 0)
        {
            throw new ValidationException(
                ErrorCodes.NNegative,
                $"Parameter 'releasesToKeep' must be >= 0, but was {releasesToKeep}.");
        }

        // Treat null collections as empty
        projects ??= Array.Empty<Project>();
        environments ??= Array.Empty<Domain.Entities.Environment>();
        releases ??= Array.Empty<Release>();
        deployments ??= Array.Empty<Deployment>();

        // Start telemetry activity (imperative shell concern)
        var stopwatch = Stopwatch.StartNew();
        using var activity = RetentionTelemetry.StartEvaluateActivity(
            releasesToKeep,
            projects.Count,
            environments.Count,
            releases.Count,
            deployments.Count);

        try
        {
            var inputs = new RetentionEvaluationInputs(
                projects, environments, releases, deployments,
                releasesToKeep, correlationId);

            var result = _engine.Evaluate(inputs);

            // Record telemetry
            stopwatch.Stop();
            RetentionTelemetry.RecordEvaluationComplete(
                activity,
                result.Diagnostics.TotalKeptReleases,
                result.Diagnostics.InvalidDeploymentsExcluded,
                result.Diagnostics.GroupsEvaluated,
                stopwatch.Elapsed.TotalMilliseconds);

            return result;
        }
        catch (Exception ex)
        {
            RetentionTelemetry.RecordError(activity, ex);
            throw;
        }
    }
}
