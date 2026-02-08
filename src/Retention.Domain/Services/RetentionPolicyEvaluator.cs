using Retention.Domain.Entities;
using Retention.Domain.Models;

namespace Retention.Domain.Services;

/// <summary>
/// Domain service that evaluates which releases should be retained based on deployment history.
/// Pure business logic with no side effects - returns deterministic results based on inputs.
/// </summary>
public sealed class RetentionPolicyEvaluator : IRetentionPolicyEvaluator
{
    private readonly IGroupRetentionEvaluator _groupEvaluator;

    public RetentionPolicyEvaluator()
        : this(new DefaultGroupRetentionEvaluator())
    {
    }

    public RetentionPolicyEvaluator(IGroupRetentionEvaluator groupEvaluator)
    {
        _groupEvaluator = groupEvaluator ?? throw new ArgumentNullException(nameof(groupEvaluator));
    }

    /// <summary>
    /// Evaluates retention policy and returns the releases to keep per project/environment combination.
    /// </summary>
    public IReadOnlyList<ReleaseCandidate> Evaluate(
        IReadOnlyDictionary<string, Release> releases,
        IReadOnlyList<Deployment> deployments,
        int releasesToKeep)
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
                continue;
            }

            var key = (release.ProjectId, deployment.EnvironmentId, deployment.ReleaseId);

            if (releaseDeployments.TryGetValue(key, out var existingLatest))
            {
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

        // Group by (ProjectId, EnvironmentId) for per-group evaluation
        var groupedByProjectEnv = releaseDeployments
            .GroupBy(kvp => (kvp.Key.ProjectId, kvp.Key.EnvironmentId))
            .ToDictionary(g => g.Key, g => g.ToList());

        var keptReleases = new List<ReleaseCandidate>();

        foreach (var (projectEnvKey, releaseEntries) in groupedByProjectEnv)
        {
            // Build group entries for the strategy
            var entries = releaseEntries
                .Select(entry =>
                {
                    var release = releases[entry.Key.ReleaseId];
                    return new GroupEntry(
                        ReleaseId: entry.Key.ReleaseId,
                        Version: release.Version,
                        Created: release.Created,
                        LatestDeployedAt: entry.Value);
                })
                .ToList();

            // Delegate to group evaluator (Template Method)
            var groupResults = _groupEvaluator.EvaluateGroup(
                projectEnvKey.ProjectId,
                projectEnvKey.EnvironmentId,
                entries,
                releasesToKeep);

            keptReleases.AddRange(groupResults);
        }

        // Return deterministic ordering: ProjectId asc, EnvironmentId asc, Rank asc
        return keptReleases
            .OrderBy(r => r.ProjectId, StringComparer.Ordinal)
            .ThenBy(r => r.EnvironmentId, StringComparer.Ordinal)
            .ThenBy(r => r.Rank)
            .ToList();
    }
}

public interface IRetentionPolicyEvaluator
{
    IReadOnlyList<ReleaseCandidate> Evaluate(
        IReadOnlyDictionary<string, Release> releases,
        IReadOnlyList<Deployment> deployments,
        int releasesToKeep);
}
