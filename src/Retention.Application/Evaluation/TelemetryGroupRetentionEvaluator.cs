using Retention.Application.Observability;
using Retention.Domain.Models;
using Retention.Domain.Services;

namespace Retention.Application.Evaluation;

/// <summary>
/// Telemetry decorator that wraps per-group evaluation with Activity spans.
/// Per-group spans cover actual ranking+selection work for meaningful duration measurement.
/// </summary>
public sealed class TelemetryGroupRetentionEvaluator : IGroupRetentionEvaluator
{
    private readonly IGroupRetentionEvaluator _inner;

    public TelemetryGroupRetentionEvaluator(IGroupRetentionEvaluator inner)
    {
        _inner = inner ?? throw new ArgumentNullException(nameof(inner));
    }

    public IReadOnlyList<ReleaseCandidate> EvaluateGroup(
        string projectId,
        string environmentId,
        IReadOnlyList<GroupEntry> entries,
        int releasesToKeep)
    {
        using var rankActivity = RetentionTelemetry.StartRankActivity(projectId, environmentId, entries.Count);

        var result = _inner.EvaluateGroup(projectId, environmentId, entries, releasesToKeep);

        RetentionTelemetry.RecordRankComplete(rankActivity, result.Count, durationMs: 0);

        return result;
    }
}
