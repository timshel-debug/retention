using System.Diagnostics;
using Retention.Application.Errors;
using Retention.Application.Models;
using Retention.Application.Observability;
using Retention.Domain.Entities;
using Retention.Domain.Services;

namespace Retention.Application;

/// <summary>
/// Application service that orchestrates retention evaluation.
/// Handles validation, invalid reference detection, and converts domain results to DTOs.
/// </summary>
public sealed class EvaluateRetentionService
{
    private readonly RetentionPolicyEvaluator _evaluator;

    public EvaluateRetentionService()
        : this(new RetentionPolicyEvaluator())
    {
    }

    public EvaluateRetentionService(RetentionPolicyEvaluator evaluator)
    {
        _evaluator = evaluator ?? throw new ArgumentNullException(nameof(evaluator));
    }

    /// <summary>
    /// Evaluates retention policy for all project/environment combinations in the input.
    /// </summary>
    /// <param name="projects">Projects to evaluate.</param>
    /// <param name="environments">Environments to evaluate.</param>
    /// <param name="releases">Releases to evaluate.</param>
    /// <param name="deployments">Deployments to evaluate.</param>
    /// <param name="releasesToKeep">Number of releases to keep per project/environment (n).</param>
    /// <param name="correlationId">Optional correlation ID for tracing; NOT generated if absent.</param>
    /// <returns>Result containing kept releases and decision log.</returns>
    /// <exception cref="ValidationException">Thrown when input validation fails.</exception>
    public RetentionResult EvaluateRetention(
        IReadOnlyList<Project>? projects,
        IReadOnlyList<Domain.Entities.Environment>? environments,
        IReadOnlyList<Release>? releases,
        IReadOnlyList<Deployment>? deployments,
        int releasesToKeep,
        string? correlationId = null)
    {
        // REQ-0009: Validate n >= 0
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

        // Start telemetry activity
        var stopwatch = Stopwatch.StartNew();
        using var activity = RetentionTelemetry.StartEvaluateActivity(
            releasesToKeep,
            projects.Count,
            environments.Count,
            releases.Count,
            deployments.Count);

        try
        {
            return EvaluateRetentionCore(
                projects, environments, releases, deployments,
                releasesToKeep, correlationId, activity, stopwatch);
        }
        catch (Exception ex)
        {
            RetentionTelemetry.RecordError(activity, ex);
            throw;
        }
    }

    private RetentionResult EvaluateRetentionCore(
        IReadOnlyList<Project> projects,
        IReadOnlyList<Domain.Entities.Environment> environments,
        IReadOnlyList<Release> releases,
        IReadOnlyList<Deployment> deployments,
        int releasesToKeep,
        string? correlationId,
        Activity? activity,
        Stopwatch stopwatch)
    {
        // Validation span (per addendum)
        using var validateActivity = RetentionTelemetry.StartValidateActivity();
        
        // Validate no null elements
        ValidateNoNullElements(projects, "projects");
        ValidateNoNullElements(environments, "environments");
        ValidateNoNullElements(releases, "releases");
        ValidateNoNullElements(deployments, "deployments");

        // Validate no duplicate IDs (ordinal comparison)
        ValidateNoDuplicateIds(projects, p => p.Id, "project", ErrorCodes.DuplicateProjectId);
        ValidateNoDuplicateIds(environments, e => e.Id, "environment", ErrorCodes.DuplicateEnvironmentId);
        ValidateNoDuplicateIds(releases, r => r.Id, "release", ErrorCodes.DuplicateReleaseId);

        // Build lookup dictionaries for valid reference checking
        var projectLookup = projects.ToDictionary(p => p.Id);
        var environmentLookup = environments.ToDictionary(e => e.Id);
        var releaseLookup = releases.ToDictionary(r => r.Id);

        // REQ-0010 / ADR-0005: Identify and exclude invalid references
        var validDeployments = new List<Deployment>();
        var diagnosticEntries = new List<DecisionLogEntry>();

        foreach (var deployment in deployments)
        {
            var invalidReasons = new List<string>();

            if (!releaseLookup.TryGetValue(deployment.ReleaseId, out var release))
            {
                invalidReasons.Add($"release '{deployment.ReleaseId}' not found");
            }
            else if (!projectLookup.ContainsKey(release.ProjectId))
            {
                invalidReasons.Add($"project '{release.ProjectId}' not found");
            }

            if (!environmentLookup.ContainsKey(deployment.EnvironmentId))
            {
                invalidReasons.Add($"environment '{deployment.EnvironmentId}' not found");
            }

            if (invalidReasons.Count > 0)
            {
                // Create diagnostic entry for this invalid deployment
                var releaseForDiag = releaseLookup.GetValueOrDefault(deployment.ReleaseId);
                diagnosticEntries.Add(new DecisionLogEntry
                {
                    ProjectId = releaseForDiag?.ProjectId ?? "unknown",
                    EnvironmentId = deployment.EnvironmentId,
                    ReleaseId = deployment.ReleaseId,
                    N = releasesToKeep,
                    Rank = 0, // Not ranked
                    LatestDeployedAt = null,
                    ReasonText = $"Deployment '{deployment.Id}' excluded: {string.Join("; ", invalidReasons)}",
                    ReasonCode = DecisionReasonCodes.InvalidReference,
                    CorrelationId = correlationId
                });
            }
            else
            {
                validDeployments.Add(deployment);
            }
        }

        // Evaluate retention policy with valid deployments only
        // Note: The onRankGroup callback is invoked by the domain layer before each group's ranking.
        // It provides an activity span for tracing but the duration measured is minimal since actual
        // ranking happens synchronously after the callback returns. This is a known limitation of the
        // callback-based telemetry approach per ADR-0007.
        var domainCandidates = _evaluator.Evaluate(
            releaseLookup,
            validDeployments,
            releasesToKeep,
            onRankGroup: (projectId, environmentId, eligibleCount, keptCount) =>
            {
                // Create activity span for tracing (duration is minimal - see note above)
                using var rankActivity = RetentionTelemetry.StartRankActivity(projectId, environmentId, eligibleCount);
                RetentionTelemetry.RecordRankComplete(rankActivity, keptCount, durationMs: 0);
            });

        // Convert domain results to DTOs
        var keptReleases = domainCandidates
            .Select(c => new KeptRelease(
                ReleaseId: c.ReleaseId,
                ProjectId: c.ProjectId,
                EnvironmentId: c.EnvironmentId,
                Version: c.Version,
                Created: c.Created,
                LatestDeployedAt: c.LatestDeployedAt,
                Rank: c.Rank,
                ReasonCode: c.ReasonCode))
            .ToList();

        // Create decision entries for kept releases
        var keptDecisions = domainCandidates
            .Select(c => new DecisionLogEntry
            {
                ProjectId = c.ProjectId,
                EnvironmentId = c.EnvironmentId,
                ReleaseId = c.ReleaseId,
                N = releasesToKeep,
                Rank = c.Rank,
                LatestDeployedAt = c.LatestDeployedAt,
                ReasonText = $"Release '{c.ReleaseId}' kept: rank {c.Rank} of {releasesToKeep} for project '{c.ProjectId}' / environment '{c.EnvironmentId}'",
                ReasonCode = DecisionReasonCodes.KeptTopN,
                CorrelationId = correlationId
            })
            .ToList();

        // Combine and sort decisions: kept before diagnostic, then by ProjectId, EnvironmentId, Rank, ReleaseId
        var allDecisions = keptDecisions.Concat(diagnosticEntries)
            .OrderBy(d => d.DecisionType == "kept" ? 0 : 1) // kept entries first
            .ThenBy(d => d.ProjectId, StringComparer.Ordinal)
            .ThenBy(d => d.EnvironmentId, StringComparer.Ordinal)
            .ThenBy(d => d.Rank)
            .ThenBy(d => d.ReleaseId, StringComparer.Ordinal)
            .ToList();

        // Count unique (ProjectId, EnvironmentId) groups that were evaluated
        var groupsEvaluated = domainCandidates
            .Select(c => (c.ProjectId, c.EnvironmentId))
            .Distinct()
            .Count();

        var diagnostics = new RetentionDiagnostics(
            GroupsEvaluated: groupsEvaluated,
            InvalidDeploymentsExcluded: diagnosticEntries.Count,
            TotalKeptReleases: keptReleases.Count);

        // Record telemetry
        stopwatch.Stop();
        RetentionTelemetry.RecordEvaluationComplete(
            activity,
            keptReleases.Count,
            diagnosticEntries.Count,
            groupsEvaluated,
            stopwatch.Elapsed.TotalMilliseconds);

        return new RetentionResult(keptReleases, allDecisions, diagnostics);
    }

    private static void ValidateNoNullElements<T>(IReadOnlyList<T> collection, string paramName) where T : class
    {
        for (int i = 0; i < collection.Count; i++)
        {
            if (collection[i] is null)
            {
                throw new ValidationException(
                    ErrorCodes.NullElement,
                    $"Null element found at index {i} in '{paramName}'.");
            }
        }
    }

    private static void ValidateNoDuplicateIds<T>(
        IReadOnlyList<T> collection,
        Func<T, string> idSelector,
        string entityType,
        string errorCode)
    {
        var seenIds = new HashSet<string>(StringComparer.Ordinal);
        var duplicates = new List<string>();

        foreach (var item in collection)
        {
            var id = idSelector(item);
            if (!seenIds.Add(id))
            {
                if (!duplicates.Contains(id))
                {
                    duplicates.Add(id);
                }
            }
        }

        if (duplicates.Count > 0)
        {
            throw new ValidationException(
                errorCode,
                $"Duplicate {entityType} ID(s) found: {string.Join(", ", duplicates)}");
        }
    }
}
