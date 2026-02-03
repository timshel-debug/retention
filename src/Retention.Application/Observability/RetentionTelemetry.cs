using System.Diagnostics;
using System.Diagnostics.Metrics;

namespace Retention.Application.Observability;

/// <summary>
/// Telemetry instrumentation for retention evaluation using System.Diagnostics.
/// Works with OpenTelemetry or any listener without requiring OTel packages.
/// Aligned with docs/12_Observability_Addendum.md specification.
/// </summary>
public static class RetentionTelemetry
{
    /// <summary>
    /// ActivitySource for distributed tracing.
    /// </summary>
    public static readonly ActivitySource ActivitySource = new("Retention", "1.0.0");
    
    /// <summary>
    /// Meter for metrics.
    /// </summary>
    public static readonly Meter Meter = new("Retention", "1.0.0");

    // Counters (aligned with addendum naming: retention_*_total)
    private static readonly Counter<long> EvaluationsCounter = 
        Meter.CreateCounter<long>("retention_evaluations_total", "evaluations", "Number of retention evaluations");
    
    private static readonly Counter<long> InvalidRecordsCounter = 
        Meter.CreateCounter<long>("retention_invalid_records_total", "records", "Number of invalid records excluded");

    // Histograms (aligned with addendum naming: retention_*_duration_ms)
    private static readonly Histogram<double> EvaluateDuration = 
        Meter.CreateHistogram<double>("retention_evaluate_duration_ms", "ms", "Duration of retention evaluation");
    
    private static readonly Histogram<double> RankDuration = 
        Meter.CreateHistogram<double>("retention_rank_duration_ms", "ms", "Duration of per-group ranking");

    // Gauge (measurement recorded on completion)
    private static readonly Histogram<long> KeptReleasesGauge = 
        Meter.CreateHistogram<long>("retention_kept_releases", "releases", "Number of kept releases per evaluation");

    /// <summary>
    /// Starts an activity span for retention evaluation.
    /// </summary>
    public static Activity? StartEvaluateActivity(
        int releasesToKeep,
        int projectsCount,
        int environmentsCount,
        int releasesCount,
        int deploymentsCount)
    {
        var activity = ActivitySource.StartActivity("retention.evaluate", ActivityKind.Internal);
        
        if (activity is not null)
        {
            activity.SetTag("retention.n", releasesToKeep);
            activity.SetTag("input.projects.count", projectsCount);
            activity.SetTag("input.environments.count", environmentsCount);
            activity.SetTag("input.releases.count", releasesCount);
            activity.SetTag("input.deployments.count", deploymentsCount);
        }

        return activity;
    }

    /// <summary>
    /// Starts a child activity span for input validation and invalid reference handling.
    /// </summary>
    public static Activity? StartValidateActivity()
    {
        return ActivitySource.StartActivity("retention.validate", ActivityKind.Internal);
    }

    /// <summary>
    /// Starts a child activity span for ranking a specific project/environment combination.
    /// </summary>
    public static Activity? StartRankActivity(string projectId, string environmentId, int eligibleCount)
    {
        var activity = ActivitySource.StartActivity("retention.rank", ActivityKind.Internal);
        
        if (activity is not null)
        {
            activity.SetTag("project.id", projectId);
            activity.SetTag("environment.id", environmentId);
            activity.SetTag("eligible_releases.count", eligibleCount);
        }

        return activity;
    }

    /// <summary>
    /// Records the kept releases count on a ranking span.
    /// </summary>
    public static void RecordRankComplete(Activity? activity, int keptCount, double durationMs)
    {
        if (activity is not null)
        {
            activity.SetTag("kept_releases.count", keptCount);
        }
        
        RankDuration.Record(durationMs);
    }

    /// <summary>
    /// Records completion metrics for an evaluation.
    /// </summary>
    public static void RecordEvaluationComplete(
        Activity? activity,
        int keptReleases,
        int invalidDeploymentsExcluded,
        int groupsEvaluated,
        double durationMs)
    {
        if (activity is not null)
        {
            activity.SetTag("retention.kept_releases", keptReleases);
            activity.SetTag("retention.invalid_deployments_excluded", invalidDeploymentsExcluded);
            activity.SetTag("retention.groups_evaluated", groupsEvaluated);
        }

        // Record metrics
        EvaluationsCounter.Add(1);
        InvalidRecordsCounter.Add(invalidDeploymentsExcluded);
        KeptReleasesGauge.Record(keptReleases);
        EvaluateDuration.Record(durationMs);
    }

    /// <summary>
    /// Records an error on the activity.
    /// </summary>
    public static void RecordError(Activity? activity, Exception exception)
    {
        if (activity is not null)
        {
            activity.SetStatus(ActivityStatusCode.Error, exception.Message);
            // Add exception details as tags (RecordException is an OTel extension, not available in base Activity)
            activity.SetTag("exception.type", exception.GetType().FullName);
            activity.SetTag("exception.message", exception.Message);
        }
    }
}
