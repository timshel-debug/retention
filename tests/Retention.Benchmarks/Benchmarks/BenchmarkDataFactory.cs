using Retention.Application.Evaluation;
using Retention.Domain.Entities;
using Environment = Retention.Domain.Entities.Environment;

namespace Retention.Benchmarks.Benchmarks;

/// <summary>
/// Deterministic dataset generator for benchmarking the retention evaluation pipeline.
/// Uses a fixed seed and stable string IDs to ensure reproducibility across runs.
/// </summary>
public static class BenchmarkDataFactory
{
    /// <summary>
    /// Builds a deterministic <see cref="RetentionEvaluationInputs"/> of the requested size.
    /// </summary>
    /// <param name="projectCount">Number of projects.</param>
    /// <param name="environmentCount">Number of environments per project.</param>
    /// <param name="releasesPerProject">Number of releases per project.</param>
    /// <param name="deploymentsPerRelease">Number of deployments per release (spread across environments).</param>
    /// <param name="invalidDeploymentRatio">Fraction of deployments referencing missing releases (0.0–1.0).</param>
    /// <param name="releasesToKeep">The N value for top-N retention.</param>
    public static RetentionEvaluationInputs BuildInputs(
        int projectCount,
        int environmentCount,
        int releasesPerProject,
        int deploymentsPerRelease,
        double invalidDeploymentRatio = 0.05,
        int releasesToKeep = 3)
    {
        var rng = new Random(42); // fixed seed for determinism

        var projects = new List<Project>(projectCount);
        for (int p = 0; p < projectCount; p++)
        {
            projects.Add(new Project($"Project-{p:D4}", $"Project {p}"));
        }

        var environments = new List<Environment>(environmentCount);
        for (int e = 0; e < environmentCount; e++)
        {
            environments.Add(new Environment($"Env-{e:D4}", $"Environment {e}"));
        }

        var releases = new List<Release>(projectCount * releasesPerProject);
        for (int p = 0; p < projectCount; p++)
        {
            for (int r = 0; r < releasesPerProject; r++)
            {
                var releaseIndex = p * releasesPerProject + r;
                releases.Add(new Release(
                    $"Release-{releaseIndex:D6}",
                    $"Project-{p:D4}",
                    $"{r + 1}.0.0",
                    new DateTimeOffset(2024, 1, 1, 0, 0, 0, TimeSpan.Zero).AddDays(releaseIndex)));
            }
        }

        var totalDeployments = releases.Count * deploymentsPerRelease;
        var invalidCount = (int)(totalDeployments * invalidDeploymentRatio);
        var deployments = new List<Deployment>(totalDeployments + invalidCount);
        int deploymentCounter = 0;

        foreach (var release in releases)
        {
            for (int d = 0; d < deploymentsPerRelease; d++)
            {
                var envIndex = rng.Next(environmentCount);
                deployments.Add(new Deployment(
                    $"Deploy-{deploymentCounter:D6}",
                    release.Id,
                    $"Env-{envIndex:D4}",
                    new DateTimeOffset(2024, 6, 1, 0, 0, 0, TimeSpan.Zero).AddHours(deploymentCounter)));
                deploymentCounter++;
            }
        }

        // Add invalid deployments referencing non-existent releases
        for (int i = 0; i < invalidCount; i++)
        {
            var envIndex = rng.Next(environmentCount);
            deployments.Add(new Deployment(
                $"Deploy-Invalid-{i:D6}",
                $"Release-MISSING-{i:D6}",
                $"Env-{envIndex:D4}",
                new DateTimeOffset(2024, 7, 1, 0, 0, 0, TimeSpan.Zero).AddHours(i)));
        }

        return new RetentionEvaluationInputs(
            projects,
            environments,
            releases,
            deployments,
            releasesToKeep,
            CorrelationId: null);
    }

    /// <summary>Small dataset: ~50 deployments.</summary>
    public static RetentionEvaluationInputs Small()
        => BuildInputs(projectCount: 5, environmentCount: 3, releasesPerProject: 3, deploymentsPerRelease: 1);

    /// <summary>Medium dataset: ~600 deployments.</summary>
    public static RetentionEvaluationInputs Medium()
        => BuildInputs(projectCount: 20, environmentCount: 5, releasesPerProject: 5, deploymentsPerRelease: 3);

    /// <summary>Large dataset: ~6,000 deployments.</summary>
    public static RetentionEvaluationInputs Large()
        => BuildInputs(projectCount: 50, environmentCount: 10, releasesPerProject: 10, deploymentsPerRelease: 6);
}
