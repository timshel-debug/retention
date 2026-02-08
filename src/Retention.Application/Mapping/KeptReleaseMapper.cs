using Retention.Application.Models;
using Retention.Domain.Models;

namespace Retention.Application.Mapping;

/// <summary>
/// Maps domain <see cref="ReleaseCandidate"/> to application <see cref="KeptRelease"/> DTOs.
/// </summary>
public interface IKeptReleaseMapper
{
    KeptRelease Map(ReleaseCandidate candidate);
}

/// <summary>
/// Default mapper: field-for-field equivalent to original inline mapping.
/// </summary>
public sealed class KeptReleaseMapper : IKeptReleaseMapper
{
    public KeptRelease Map(ReleaseCandidate candidate)
    {
        return new KeptRelease(
            ReleaseId: candidate.ReleaseId,
            ProjectId: candidate.ProjectId,
            EnvironmentId: candidate.EnvironmentId,
            Version: candidate.Version,
            Created: candidate.Created,
            LatestDeployedAt: candidate.LatestDeployedAt,
            Rank: candidate.Rank,
            ReasonCode: candidate.ReasonCode);
    }
}
