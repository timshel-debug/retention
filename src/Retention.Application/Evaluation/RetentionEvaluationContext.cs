using Retention.Application.Indexing;
using Retention.Application.Models;
using Retention.Domain.Entities;
using Retention.Domain.Models;
using Environment = Retention.Domain.Entities.Environment;

namespace Retention.Application.Evaluation;

/// <summary>
/// Mutable context threaded through the evaluation pipeline steps.
/// </summary>
public sealed class RetentionEvaluationContext
{
    // ── Inputs ──
    public IReadOnlyList<Project> Projects { get; set; } = Array.Empty<Project>();
    public IReadOnlyList<Environment> Environments { get; set; } = Array.Empty<Environment>();
    public IReadOnlyList<Release> Releases { get; set; } = Array.Empty<Release>();
    public IReadOnlyList<Deployment> Deployments { get; set; } = Array.Empty<Deployment>();
    public int ReleasesToKeep { get; set; }
    public string? CorrelationId { get; set; }

    // ── Derived ──
    public ReferenceIndex? ReferenceIndex { get; set; }
    public IReadOnlyList<Deployment> ValidDeployments { get; set; } = Array.Empty<Deployment>();
    public List<DecisionLogEntry> DiagnosticDecisionEntries { get; set; } = new();
    public int InvalidExcludedCount { get; set; }

    // ── Domain ──
    public IReadOnlyList<ReleaseCandidate> DomainCandidates { get; set; } = Array.Empty<ReleaseCandidate>();

    // ── Outputs ──
    public List<KeptRelease> KeptReleases { get; set; } = new();
    public List<DecisionLogEntry> KeptDecisionEntries { get; set; } = new();
    public List<DecisionLogEntry> AllDecisionEntries { get; set; } = new();
    public RetentionDiagnostics? Diagnostics { get; set; }
    public RetentionResult? Result { get; set; }
}
