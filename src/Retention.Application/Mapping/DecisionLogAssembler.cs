using Retention.Application.Models;
using Retention.Domain.Entities;
using Retention.Domain.Models;

namespace Retention.Application.Mapping;

/// <summary>
/// Builds decision log entries for kept releases and invalid deployments.
/// </summary>
public interface IDecisionLogAssembler
{
    /// <summary>
    /// Creates a "kept" decision entry for a retained release candidate.
    /// </summary>
    DecisionLogEntry BuildKeptEntry(ReleaseCandidate candidate, int releasesToKeep, string? correlationId);

    /// <summary>
    /// Creates a "diagnostic" decision entry for an excluded invalid deployment.
    /// </summary>
    DecisionLogEntry BuildInvalidDeploymentEntry(
        Deployment deployment,
        string projectId,
        int releasesToKeep,
        IReadOnlyList<string> reasons,
        string? correlationId);
}

/// <summary>
/// Default assembler: preserves exact ReasonText formatting from original implementation.
/// </summary>
public sealed class DecisionLogAssembler : IDecisionLogAssembler
{
    public DecisionLogEntry BuildKeptEntry(ReleaseCandidate candidate, int releasesToKeep, string? correlationId)
    {
        return new DecisionLogEntry
        {
            ProjectId = candidate.ProjectId,
            EnvironmentId = candidate.EnvironmentId,
            ReleaseId = candidate.ReleaseId,
            N = releasesToKeep,
            Rank = candidate.Rank,
            LatestDeployedAt = candidate.LatestDeployedAt,
            ReasonText = $"Release '{candidate.ReleaseId}' kept: rank {candidate.Rank} of {releasesToKeep} for project '{candidate.ProjectId}' / environment '{candidate.EnvironmentId}'",
            ReasonCode = DecisionReasonCodes.KeptTopN,
            CorrelationId = correlationId
        };
    }

    public DecisionLogEntry BuildInvalidDeploymentEntry(
        Deployment deployment,
        string projectId,
        int releasesToKeep,
        IReadOnlyList<string> reasons,
        string? correlationId)
    {
        return new DecisionLogEntry
        {
            ProjectId = projectId,
            EnvironmentId = deployment.EnvironmentId,
            ReleaseId = deployment.ReleaseId,
            N = releasesToKeep,
            Rank = 0,
            LatestDeployedAt = null,
            ReasonText = $"Deployment '{deployment.Id}' excluded: {string.Join("; ", reasons)}",
            ReasonCode = DecisionReasonCodes.InvalidReference,
            CorrelationId = correlationId
        };
    }
}
