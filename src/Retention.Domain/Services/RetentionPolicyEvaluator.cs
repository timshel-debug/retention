using Retention.Domain.Entities;
using Retention.Domain.Models;

namespace Retention.Domain.Services;

/// <summary>
/// Domain service that evaluates which releases should be retained based on deployment history.
/// Pure business logic with no side effects - returns deterministic results based on inputs.
/// </summary>
public sealed class RetentionPolicyEvaluator
{
    /// <summary>
    /// Evaluates retention policy and returns the releases to keep per project/environment combination.
    /// </summary>
    /// <param name="releases">All releases indexed by ReleaseId for lookup.</param>
    /// <param name="deployments">Valid deployments to evaluate (caller should pre-filter invalid references).</param>
    /// <param name="releasesToKeep">Number of releases to keep per project/environment (n >= 0).</param>
    /// <param name="onRankGroup">Optional callback for telemetry/observability per ranking group; does not affect behavior.</param>
    /// <returns>Deterministically ordered list of release candidates to keep.</returns>
    public IReadOnlyList<ReleaseCandidate> Evaluate(
        IReadOnlyDictionary<string, Release> releases,
        IReadOnlyList<Deployment> deployments,
        int releasesToKeep,
        Action<string, string, int, int>? onRankGroup = null)
    {
        ArgumentNullException.ThrowIfNull(releases);
        ArgumentNullException.ThrowIfNull(deployments);
        
        if (releasesToKeep == 0)
        {
            return Array.Empty<ReleaseCandidate>();
        }

        // Group deployments by (ProjectId, EnvironmentId, ReleaseId) and compute LatestDeployedAt
        var releaseDeployments = new Dictionary<(string ProjectId, string EnvironmentId, string ReleaseId), DateTimeOffset>();

        foreach (var deployment in deployments)
        {
            if (!releases.TryGetValue(deployment.ReleaseId, out var release))
            {
                // Skip deployments for releases not in the lookup (should be pre-filtered, but defensive)
                continue;
            }

            var key = (release.ProjectId, deployment.EnvironmentId, deployment.ReleaseId);

            if (releaseDeployments.TryGetValue(key, out var existingLatest))
            {
                // Keep the maximum DeployedAt (REQ-0004)
                if (deployment.DeployedAt > existingLatest)
                {
                    releaseDeployments[key] = deployment.DeployedAt;
                }
            }
            else
            {
                releaseDeployments[key] = deployment.DeployedAt;
            }
        }

        // Group by (ProjectId, EnvironmentId) for ranking
        var groupedByProjectEnv = releaseDeployments
            .GroupBy(kvp => (kvp.Key.ProjectId, kvp.Key.EnvironmentId))
            .ToDictionary(g => g.Key, g => g.ToList());

        var keptReleases = new List<ReleaseCandidate>();

        foreach (var (projectEnvKey, releaseEntries) in groupedByProjectEnv)
        {
            // Invoke telemetry callback if provided (does not affect domain logic)
            var eligibleCount = releaseEntries.Count;
            var keptCount = Math.Min(eligibleCount, releasesToKeep);
            onRankGroup?.Invoke(projectEnvKey.ProjectId, projectEnvKey.EnvironmentId, eligibleCount, keptCount);
            
            // Sort by tie-breakers (ADR-0003):
            // 1. LatestDeployedAt desc
            // 2. Release.Created desc
            // 3. Release.Id asc (ordinal)
            var sorted = releaseEntries
                .Select(entry =>
                {
                    var release = releases[entry.Key.ReleaseId];
                    return new
                    {
                        entry.Key.ReleaseId,
                        release.Version,
                        release.Created,
                        LatestDeployedAt = entry.Value
                    };
                })
                .OrderByDescending(x => x.LatestDeployedAt)
                .ThenByDescending(x => x.Created)
                .ThenBy(x => x.ReleaseId, StringComparer.Ordinal)
                .ToList();

            // Select top n (REQ-0003)
            var topN = sorted.Take(releasesToKeep);

            int rank = 1;
            foreach (var candidate in topN)
            {
                keptReleases.Add(new ReleaseCandidate(
                    ProjectId: projectEnvKey.ProjectId,
                    EnvironmentId: projectEnvKey.EnvironmentId,
                    ReleaseId: candidate.ReleaseId,
                    Version: candidate.Version,
                    Created: candidate.Created,
                    LatestDeployedAt: candidate.LatestDeployedAt,
                    Rank: rank++,
                    ReasonCode: ReasonCodes.KeptTopN));
            }
        }

        // Return deterministic ordering: ProjectId asc, EnvironmentId asc, Rank asc
        return keptReleases
            .OrderBy(r => r.ProjectId, StringComparer.Ordinal)
            .ThenBy(r => r.EnvironmentId, StringComparer.Ordinal)
            .ThenBy(r => r.Rank)
            .ToList();
    }
}
