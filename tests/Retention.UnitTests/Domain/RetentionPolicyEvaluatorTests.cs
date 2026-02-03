using FluentAssertions;
using Retention.Domain.Entities;
using Retention.Domain.Models;
using Retention.Domain.Services;

namespace Retention.UnitTests.Domain;

public class RetentionPolicyEvaluatorTests
{
    private readonly RetentionPolicyEvaluator _evaluator = new();

    #region Test Helpers

    private static Dictionary<string, Release> CreateReleaseLookup(params Release[] releases)
        => releases.ToDictionary(r => r.Id);

    private static DateTimeOffset Date(int year, int month, int day, int hour = 0)
        => new(year, month, day, hour, 0, 0, TimeSpan.Zero);

    #endregion

    #region REQ-0002: Eligibility requires deployment

    [Fact]
    public void ReleaseWithNoDeployments_IsNotKept()
    {
        // Arrange
        var releases = CreateReleaseLookup(
            new Release("Release-1", "Project-1", "1.0.0", Date(2000, 1, 1, 8)));
        
        var deployments = Array.Empty<Deployment>();

        // Act
        var result = _evaluator.Evaluate(releases, deployments, releasesToKeep: 1);

        // Assert
        result.Should().BeEmpty("releases with zero deployments are not eligible");
    }

    [Fact]
    public void ReleaseWithDeploymentToOtherEnvironment_IsNotKeptForThisEnvironment()
    {
        // Arrange
        var releases = CreateReleaseLookup(
            new Release("Release-1", "Project-1", "1.0.0", Date(2000, 1, 1, 8)));
        
        var deployments = new[]
        {
            new Deployment("Deployment-1", "Release-1", "Environment-2", Date(2000, 1, 1, 10))
        };

        // Act
        var result = _evaluator.Evaluate(releases, deployments, releasesToKeep: 1);

        // Assert
        result.Should().HaveCount(1);
        result[0].EnvironmentId.Should().Be("Environment-2", "release only deployed to Environment-2");
    }

    #endregion

    #region REQ-0003/0004: Latest deployment timestamp determines ranking

    [Fact]
    public void SingleRelease_KeepOne_IsKept()
    {
        // Sample Test Case 1: 1 Release, Keep 1
        var releases = CreateReleaseLookup(
            new Release("Release-1", "Project-1", "1.0.0", Date(2000, 1, 1, 8)));
        
        var deployments = new[]
        {
            new Deployment("Deployment-1", "Release-1", "Environment-1", Date(2000, 1, 1, 10))
        };

        var result = _evaluator.Evaluate(releases, deployments, releasesToKeep: 1);

        result.Should().HaveCount(1);
        result[0].ReleaseId.Should().Be("Release-1");
        result[0].ReasonCode.Should().Be(ReasonCodes.KeptTopN);
        result[0].Rank.Should().Be(1);
    }

    [Fact]
    public void TwoReleases_SameEnvironment_KeepOne_MostRecentlyDeployedIsKept()
    {
        // Sample Test Case 2: 2 Releases deployed to the same environment, Keep 1
        var releases = CreateReleaseLookup(
            new Release("Release-1", "Project-1", "1.0.0", Date(2000, 1, 1, 8)),
            new Release("Release-2", "Project-1", "1.0.1", Date(2000, 1, 1, 9)));
        
        var deployments = new[]
        {
            new Deployment("Deployment-2", "Release-1", "Environment-1", Date(2000, 1, 1, 11)),
            new Deployment("Deployment-1", "Release-2", "Environment-1", Date(2000, 1, 1, 10))
        };

        var result = _evaluator.Evaluate(releases, deployments, releasesToKeep: 1);

        result.Should().HaveCount(1);
        result[0].ReleaseId.Should().Be("Release-1", "it was deployed most recently at 11:00");
    }

    [Fact]
    public void ReleaseWithMultipleDeployments_UsesMaxDeployedAt()
    {
        // REQ-0004: max(DeployedAt) for a release within project/environment
        var releases = CreateReleaseLookup(
            new Release("Release-1", "Project-1", "1.0.0", Date(2000, 1, 1, 8)),
            new Release("Release-2", "Project-1", "1.0.1", Date(2000, 1, 1, 9)));
        
        var deployments = new[]
        {
            // Release-1 deployed twice, with max at 09:00
            new Deployment("Deployment-1", "Release-1", "Environment-1", Date(2000, 1, 1, 7)),
            new Deployment("Deployment-2", "Release-1", "Environment-1", Date(2000, 1, 1, 9)),
            // Release-2 deployed once at 10:00
            new Deployment("Deployment-3", "Release-2", "Environment-1", Date(2000, 1, 1, 10))
        };

        var result = _evaluator.Evaluate(releases, deployments, releasesToKeep: 1);

        result.Should().HaveCount(1);
        result[0].ReleaseId.Should().Be("Release-2", "Release-2 at 10:00 > Release-1 max at 09:00");
        result[0].LatestDeployedAt.Should().Be(Date(2000, 1, 1, 10));
    }

    [Fact]
    public void KeepTwo_ReturnsTopTwoByDeploymentTime()
    {
        var releases = CreateReleaseLookup(
            new Release("Release-1", "Project-1", "1.0.0", Date(2000, 1, 1, 8)),
            new Release("Release-2", "Project-1", "1.0.1", Date(2000, 1, 1, 9)),
            new Release("Release-3", "Project-1", "1.0.2", Date(2000, 1, 1, 10)));
        
        var deployments = new[]
        {
            new Deployment("D-1", "Release-1", "Environment-1", Date(2000, 1, 1, 12)),
            new Deployment("D-2", "Release-2", "Environment-1", Date(2000, 1, 1, 14)),
            new Deployment("D-3", "Release-3", "Environment-1", Date(2000, 1, 1, 13))
        };

        var result = _evaluator.Evaluate(releases, deployments, releasesToKeep: 2);

        result.Should().HaveCount(2);
        result[0].ReleaseId.Should().Be("Release-2", "rank 1: deployed at 14:00");
        result[0].Rank.Should().Be(1);
        result[1].ReleaseId.Should().Be("Release-3", "rank 2: deployed at 13:00");
        result[1].Rank.Should().Be(2);
    }

    #endregion

    #region ADR-0003: Deterministic tie-breakers

    [Fact]
    public void SameLatestDeployedAt_TieBreaker_CreatedDescWins()
    {
        // Same LatestDeployedAt => Release.Created desc wins
        var releases = CreateReleaseLookup(
            new Release("Release-1", "Project-1", "1.0.0", Date(2000, 1, 1, 8)),
            new Release("Release-2", "Project-1", "1.0.1", Date(2000, 1, 1, 10))); // Created later
        
        var deployments = new[]
        {
            new Deployment("D-1", "Release-1", "Environment-1", Date(2000, 1, 2, 12)),
            new Deployment("D-2", "Release-2", "Environment-1", Date(2000, 1, 2, 12)) // Same time
        };

        var result = _evaluator.Evaluate(releases, deployments, releasesToKeep: 1);

        result.Should().HaveCount(1);
        result[0].ReleaseId.Should().Be("Release-2", "Release-2 was created later (10:00 > 8:00)");
    }

    [Fact]
    public void SameLatestDeployedAtAndCreated_TieBreaker_ReleaseIdAscWins()
    {
        // Same LatestDeployedAt & Created => ReleaseId asc wins
        var releases = CreateReleaseLookup(
            new Release("Release-A", "Project-1", "1.0.0", Date(2000, 1, 1, 8)),
            new Release("Release-B", "Project-1", "1.0.1", Date(2000, 1, 1, 8))); // Same Created
        
        var deployments = new[]
        {
            new Deployment("D-1", "Release-A", "Environment-1", Date(2000, 1, 2, 12)),
            new Deployment("D-2", "Release-B", "Environment-1", Date(2000, 1, 2, 12)) // Same time
        };

        var result = _evaluator.Evaluate(releases, deployments, releasesToKeep: 1);

        result.Should().HaveCount(1);
        result[0].ReleaseId.Should().Be("Release-A", "Release-A < Release-B alphabetically (asc)");
    }

    [Fact]
    public void TieBreaker_NumericIdsSortedOrdinallyNotNumerically()
    {
        // Ordinal sorting: "Release-10" < "Release-2" (string comparison)
        var releases = CreateReleaseLookup(
            new Release("Release-10", "Project-1", "1.0.0", Date(2000, 1, 1, 8)),
            new Release("Release-2", "Project-1", "1.0.1", Date(2000, 1, 1, 8)));
        
        var deployments = new[]
        {
            new Deployment("D-1", "Release-10", "Environment-1", Date(2000, 1, 2, 12)),
            new Deployment("D-2", "Release-2", "Environment-1", Date(2000, 1, 2, 12))
        };

        var result = _evaluator.Evaluate(releases, deployments, releasesToKeep: 1);

        result.Should().HaveCount(1);
        result[0].ReleaseId.Should().Be("Release-10", "'Release-10' < 'Release-2' in ordinal comparison");
    }

    #endregion

    #region REQ-0006: Multi-environment evaluation

    [Fact]
    public void TwoReleases_DifferentEnvironments_KeepOne_BothKept()
    {
        // Sample Test Case 3: 2 Releases deployed to different environments, Keep 1
        var releases = CreateReleaseLookup(
            new Release("Release-1", "Project-1", "1.0.0", Date(2000, 1, 1, 8)),
            new Release("Release-2", "Project-1", "1.0.1", Date(2000, 1, 1, 9)));
        
        var deployments = new[]
        {
            new Deployment("Deployment-2", "Release-1", "Environment-2", Date(2000, 1, 2, 11)),
            new Deployment("Deployment-1", "Release-2", "Environment-1", Date(2000, 1, 1, 10))
        };

        var result = _evaluator.Evaluate(releases, deployments, releasesToKeep: 1);

        result.Should().HaveCount(2, "each environment keeps its top 1");
        
        var env1Kept = result.Single(r => r.EnvironmentId == "Environment-1");
        var env2Kept = result.Single(r => r.EnvironmentId == "Environment-2");
        
        env1Kept.ReleaseId.Should().Be("Release-2", "most recently deployed to Environment-1");
        env2Kept.ReleaseId.Should().Be("Release-1", "most recently deployed to Environment-2");
    }

    [Fact]
    public void MultipleProjects_EvaluatedIndependently()
    {
        var releases = CreateReleaseLookup(
            new Release("Release-1", "Project-A", "1.0.0", Date(2000, 1, 1, 8)),
            new Release("Release-2", "Project-A", "1.0.1", Date(2000, 1, 1, 9)),
            new Release("Release-3", "Project-B", "1.0.0", Date(2000, 1, 1, 10)));
        
        var deployments = new[]
        {
            new Deployment("D-1", "Release-1", "Environment-1", Date(2000, 1, 1, 12)),
            new Deployment("D-2", "Release-2", "Environment-1", Date(2000, 1, 1, 11)),
            new Deployment("D-3", "Release-3", "Environment-1", Date(2000, 1, 1, 13))
        };

        var result = _evaluator.Evaluate(releases, deployments, releasesToKeep: 1);

        result.Should().HaveCount(2, "one kept per project/environment");
        result.Should().Contain(r => r.ProjectId == "Project-A" && r.ReleaseId == "Release-1");
        result.Should().Contain(r => r.ProjectId == "Project-B" && r.ReleaseId == "Release-3");
    }

    #endregion

    #region REQ-0009: n = 0 keeps nothing

    [Fact]
    public void KeepZero_ReturnsEmpty()
    {
        var releases = CreateReleaseLookup(
            new Release("Release-1", "Project-1", "1.0.0", Date(2000, 1, 1, 8)));
        
        var deployments = new[]
        {
            new Deployment("D-1", "Release-1", "Environment-1", Date(2000, 1, 1, 10))
        };

        var result = _evaluator.Evaluate(releases, deployments, releasesToKeep: 0);

        result.Should().BeEmpty("n = 0 means keep nothing");
    }

    #endregion

    #region NFR-0003: Determinism

    [Fact]
    public void ShuffledInputs_ProduceSameOutput()
    {
        var releases = CreateReleaseLookup(
            new Release("Release-1", "Project-1", "1.0.0", Date(2000, 1, 1, 8)),
            new Release("Release-2", "Project-1", "1.0.1", Date(2000, 1, 1, 9)),
            new Release("Release-3", "Project-1", "1.0.2", Date(2000, 1, 1, 10)));
        
        var deployments1 = new[]
        {
            new Deployment("D-1", "Release-1", "Environment-1", Date(2000, 1, 1, 12)),
            new Deployment("D-2", "Release-2", "Environment-1", Date(2000, 1, 1, 14)),
            new Deployment("D-3", "Release-3", "Environment-1", Date(2000, 1, 1, 13))
        };

        var deployments2 = new[]
        {
            new Deployment("D-3", "Release-3", "Environment-1", Date(2000, 1, 1, 13)),
            new Deployment("D-1", "Release-1", "Environment-1", Date(2000, 1, 1, 12)),
            new Deployment("D-2", "Release-2", "Environment-1", Date(2000, 1, 1, 14))
        };

        var result1 = _evaluator.Evaluate(releases, deployments1, releasesToKeep: 2);
        var result2 = _evaluator.Evaluate(releases, deployments2, releasesToKeep: 2);

        result1.Should().BeEquivalentTo(result2, options => options.WithStrictOrdering(),
            "shuffled inputs must produce identical ordered output");
    }

    [Fact]
    public void RepeatedEvaluations_ProduceIdenticalResults()
    {
        var releases = CreateReleaseLookup(
            new Release("Release-1", "Project-1", "1.0.0", Date(2000, 1, 1, 8)),
            new Release("Release-2", "Project-1", "1.0.1", Date(2000, 1, 1, 9)));
        
        var deployments = new[]
        {
            new Deployment("D-1", "Release-1", "Environment-1", Date(2000, 1, 1, 10)),
            new Deployment("D-2", "Release-2", "Environment-1", Date(2000, 1, 1, 10)) // Same time - tie-breaker
        };

        var results = Enumerable.Range(0, 10)
            .Select(_ => _evaluator.Evaluate(releases, deployments, releasesToKeep: 1))
            .ToList();

        results.Should().AllSatisfy(r =>
        {
            r.Should().BeEquivalentTo(results[0], options => options.WithStrictOrdering());
        });
    }

    #endregion

    #region Output ordering

    [Fact]
    public void OutputOrdering_ProjectIdAsc_EnvironmentIdAsc_RankAsc()
    {
        var releases = CreateReleaseLookup(
            new Release("R-1", "Project-B", "1.0", Date(2000, 1, 1)),
            new Release("R-2", "Project-B", "1.1", Date(2000, 1, 2)),
            new Release("R-3", "Project-A", "1.0", Date(2000, 1, 1)),
            new Release("R-4", "Project-A", "1.1", Date(2000, 1, 2)));
        
        var deployments = new[]
        {
            new Deployment("D-1", "R-1", "Environment-1", Date(2000, 1, 3)),
            new Deployment("D-2", "R-2", "Environment-1", Date(2000, 1, 4)),
            new Deployment("D-3", "R-3", "Environment-1", Date(2000, 1, 5)),
            new Deployment("D-4", "R-4", "Environment-1", Date(2000, 1, 6))
        };

        var result = _evaluator.Evaluate(releases, deployments, releasesToKeep: 2);

        // Expected order: Project-A/Env-1 (rank 1, 2), then Project-B/Env-1 (rank 1, 2)
        result.Should().HaveCount(4);
        
        result[0].ProjectId.Should().Be("Project-A");
        result[0].Rank.Should().Be(1);
        result[0].ReleaseId.Should().Be("R-4"); // Most recent for Project-A
        
        result[1].ProjectId.Should().Be("Project-A");
        result[1].Rank.Should().Be(2);
        result[1].ReleaseId.Should().Be("R-3");
        
        result[2].ProjectId.Should().Be("Project-B");
        result[2].Rank.Should().Be(1);
        result[2].ReleaseId.Should().Be("R-2"); // Most recent for Project-B
        
        result[3].ProjectId.Should().Be("Project-B");
        result[3].Rank.Should().Be(2);
        result[3].ReleaseId.Should().Be("R-1");
    }

    #endregion
}
