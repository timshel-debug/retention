using Retention.Application.Indexing;
using Retention.Domain.Entities;

namespace Retention.Application.Specifications;

/// <summary>
/// Determines whether a deployment has valid references to existing releases,
/// projects, and environments.
/// </summary>
public interface IDeploymentValiditySpecification
{
    DeploymentValidityResult Evaluate(Deployment deployment, ReferenceIndex index);
}

/// <summary>
/// Default implementation matching the original validation logic:
/// 1. Release must exist for deployment.ReleaseId
/// 2. If release exists, release.ProjectId must exist in projects
/// 3. Environment must exist for deployment.EnvironmentId
/// Reasons are appended in the same order as original implementation.
/// </summary>
public sealed class DefaultDeploymentValiditySpecification : IDeploymentValiditySpecification
{
    public DeploymentValidityResult Evaluate(Deployment deployment, ReferenceIndex index)
    {
        ArgumentNullException.ThrowIfNull(deployment);
        ArgumentNullException.ThrowIfNull(index);

        var reasons = new List<string>();

        if (!index.ReleasesById.TryGetValue(deployment.ReleaseId, out var release))
        {
            reasons.Add($"release '{deployment.ReleaseId}' not found");
        }
        else if (!index.ProjectsById.ContainsKey(release.ProjectId))
        {
            reasons.Add($"project '{release.ProjectId}' not found");
        }

        if (!index.EnvironmentsById.ContainsKey(deployment.EnvironmentId))
        {
            reasons.Add($"environment '{deployment.EnvironmentId}' not found");
        }

        return reasons.Count > 0
            ? DeploymentValidityResult.Invalid(reasons)
            : DeploymentValidityResult.Valid();
    }
}
