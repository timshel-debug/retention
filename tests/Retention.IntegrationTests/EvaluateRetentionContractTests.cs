using System.Text.Json;
using System.Text.Json.Serialization;
using FluentAssertions;
using Retention.Application;
using Retention.Application.Models;
using Retention.Domain.Entities;
using Environment = Retention.Domain.Entities.Environment;

namespace Retention.IntegrationTests;

/// <summary>
/// Contract tests validating the public EvaluateRetention API behavior, DTO shapes, and deterministic ordering.
/// </summary>
public class EvaluateRetentionContractTests
{
    private readonly EvaluateRetentionService _service = new();

    #region Test Helpers

    private static DateTimeOffset Date(int year, int month, int day, int hour = 0)
        => new(year, month, day, hour, 0, 0, TimeSpan.Zero);

    #endregion

    #region Contract: DTO shapes

    [Fact]
    public void RetentionResult_HasExpectedShape()
    {
        var projects = new[] { new Project("P1", "Project 1") };
        var environments = new[] { new Environment("E1", "Env 1") };
        var releases = new[] { new Release("R1", "P1", "1.0.0", Date(2000, 1, 1)) };
        var deployments = new[] { new Deployment("D1", "R1", "E1", Date(2000, 1, 2)) };

        var result = _service.EvaluateRetention(
            projects, environments, releases, deployments,
            releasesToKeep: 1);

        // Validate RetentionResult shape
        result.Should().NotBeNull();
        result.KeptReleases.Should().NotBeNull();
        result.Decisions.Should().NotBeNull();
        result.Diagnostics.Should().NotBeNull();
        
        // Validate KeptRelease shape
        var kept = result.KeptReleases.Single();
        kept.ReleaseId.Should().NotBeNullOrEmpty();
        kept.ProjectId.Should().NotBeNullOrEmpty();
        kept.EnvironmentId.Should().NotBeNullOrEmpty();
        kept.Created.Should().NotBe(default);
        kept.LatestDeployedAt.Should().NotBe(default);
        kept.Rank.Should().BePositive();
        kept.ReasonCode.Should().NotBeNullOrEmpty();
        
        // Validate DecisionLogEntry shape
        var decision = result.Decisions.Single();
        decision.ProjectId.Should().NotBeNullOrEmpty();
        decision.EnvironmentId.Should().NotBeNullOrEmpty();
        decision.ReleaseId.Should().NotBeNullOrEmpty();
        decision.N.Should().BeGreaterThanOrEqualTo(0);
        decision.Rank.Should().BeGreaterThanOrEqualTo(0);
        decision.ReasonText.Should().NotBeNullOrEmpty();
        decision.ReasonCode.Should().NotBeNullOrEmpty();
        
        // Validate RetentionDiagnostics shape
        result.Diagnostics.GroupsEvaluated.Should().BeGreaterThanOrEqualTo(0);
        result.Diagnostics.InvalidDeploymentsExcluded.Should().BeGreaterThanOrEqualTo(0);
        result.Diagnostics.TotalKeptReleases.Should().BeGreaterThanOrEqualTo(0);
    }

    #endregion

    #region Contract: Deterministic ordering

    [Fact]
    public void KeptReleases_OrderedDeterministically()
    {
        var projects = new[]
        {
            new Project("P-B", "Project B"),
            new Project("P-A", "Project A")
        };
        var environments = new[]
        {
            new Environment("E-2", "Env 2"),
            new Environment("E-1", "Env 1")
        };
        var releases = new[]
        {
            new Release("R1", "P-A", "1.0", Date(2000, 1, 1)),
            new Release("R2", "P-A", "1.1", Date(2000, 1, 2)),
            new Release("R3", "P-B", "1.0", Date(2000, 1, 1)),
            new Release("R4", "P-B", "1.1", Date(2000, 1, 2))
        };
        var deployments = new[]
        {
            new Deployment("D1", "R1", "E-1", Date(2000, 1, 3)),
            new Deployment("D2", "R2", "E-1", Date(2000, 1, 4)),
            new Deployment("D3", "R3", "E-2", Date(2000, 1, 3)),
            new Deployment("D4", "R4", "E-2", Date(2000, 1, 4))
        };

        var result = _service.EvaluateRetention(
            projects, environments, releases, deployments,
            releasesToKeep: 2);

        // Should be ordered by: ProjectId asc, EnvironmentId asc, Rank asc
        result.KeptReleases.Should().SatisfyRespectively(
            r => { r.ProjectId.Should().Be("P-A"); r.EnvironmentId.Should().Be("E-1"); r.Rank.Should().Be(1); },
            r => { r.ProjectId.Should().Be("P-A"); r.EnvironmentId.Should().Be("E-1"); r.Rank.Should().Be(2); },
            r => { r.ProjectId.Should().Be("P-B"); r.EnvironmentId.Should().Be("E-2"); r.Rank.Should().Be(1); },
            r => { r.ProjectId.Should().Be("P-B"); r.EnvironmentId.Should().Be("E-2"); r.Rank.Should().Be(2); });
    }

    [Fact]
    public void Decisions_KeptBeforeDiagnostic_ThenOrderedDeterministically()
    {
        var projects = new[] { new Project("P1", "Project 1") };
        var environments = new[] { new Environment("E1", "Env 1") };
        var releases = new[]
        {
            new Release("R1", "P1", "1.0", Date(2000, 1, 1)),
            new Release("R2", "P1", "1.1", Date(2000, 1, 2))
        };
        var deployments = new[]
        {
            new Deployment("D1", "R1", "E1", Date(2000, 1, 3)),
            new Deployment("D2", "R2", "E1", Date(2000, 1, 4)),
            new Deployment("D-INVALID", "R-MISSING", "E1", Date(2000, 1, 5))
        };

        var result = _service.EvaluateRetention(
            projects, environments, releases, deployments,
            releasesToKeep: 2);

        // Kept decisions should come before diagnostic
        var keptDecisions = result.Decisions.Where(d => d.ReasonCode == DecisionReasonCodes.KeptTopN).ToList();
        var diagnosticDecisions = result.Decisions.Where(d => d.ReasonCode == DecisionReasonCodes.InvalidReference).ToList();
        
        keptDecisions.Should().HaveCount(2);
        diagnosticDecisions.Should().HaveCount(1);
        
        // Verify ordering: all kept before any diagnostic
        var firstDiagnosticIndex = result.Decisions.ToList().FindIndex(d => d.ReasonCode == DecisionReasonCodes.InvalidReference);
        var lastKeptIndex = result.Decisions.ToList().FindLastIndex(d => d.ReasonCode == DecisionReasonCodes.KeptTopN);
        
        lastKeptIndex.Should().BeLessThan(firstDiagnosticIndex, "kept entries should come before diagnostic entries");
    }

    #endregion

    #region Contract: Stable reason codes

    [Fact]
    public void ReasonCodes_AreStableStrings()
    {
        var projects = new[] { new Project("P1", "Project 1") };
        var environments = new[] { new Environment("E1", "Env 1") };
        var releases = new[] { new Release("R1", "P1", "1.0", Date(2000, 1, 1)) };
        var deployments = new[]
        {
            new Deployment("D1", "R1", "E1", Date(2000, 1, 2)),
            new Deployment("D-INVALID", "R-MISSING", "E1", Date(2000, 1, 3))
        };

        var result = _service.EvaluateRetention(
            projects, environments, releases, deployments,
            releasesToKeep: 1);

        // Verify stable reason codes
        result.KeptReleases.Single().ReasonCode.Should().Be("kept.top_n");
        result.Decisions.Where(d => d.ReasonCode == "kept.top_n").Should().HaveCount(1);
        result.Decisions.Where(d => d.ReasonCode == "diagnostic.invalid_reference").Should().HaveCount(1);
    }

    #endregion

    #region Contract: Multi-project multi-environment

    [Fact]
    public void MultiProjectMultiEnvironment_EvaluatesAllCombinations()
    {
        var projects = new[]
        {
            new Project("Project-1", "Random Quotes"),
            new Project("Project-2", "Pet Shop")
        };
        var environments = new[]
        {
            new Environment("Environment-1", "Staging"),
            new Environment("Environment-2", "Production")
        };
        var releases = new[]
        {
            new Release("Release-1", "Project-1", "1.0.0", Date(2000, 1, 1, 9)),
            new Release("Release-2", "Project-1", "1.0.1", Date(2000, 1, 2, 9)),
            new Release("Release-3", "Project-2", "1.0.0", Date(2000, 1, 1, 9)),
            new Release("Release-4", "Project-2", "1.0.1", Date(2000, 1, 2, 9))
        };
        var deployments = new[]
        {
            // Project-1 to Environment-1
            new Deployment("D1", "Release-1", "Environment-1", Date(2000, 1, 1, 10)),
            new Deployment("D2", "Release-2", "Environment-1", Date(2000, 1, 2, 10)),
            // Project-1 to Environment-2
            new Deployment("D3", "Release-1", "Environment-2", Date(2000, 1, 1, 11)),
            // Project-2 to Environment-1
            new Deployment("D4", "Release-3", "Environment-1", Date(2000, 1, 1, 10)),
            new Deployment("D5", "Release-4", "Environment-1", Date(2000, 1, 2, 10))
        };

        var result = _service.EvaluateRetention(
            projects, environments, releases, deployments,
            releasesToKeep: 1);

        // 3 groups: (P1, E1), (P1, E2), (P2, E1)
        result.Diagnostics.GroupsEvaluated.Should().Be(3);
        result.KeptReleases.Should().HaveCount(3);
        
        // Verify correct releases kept for each group
        result.KeptReleases.Should().Contain(r => 
            r.ProjectId == "Project-1" && r.EnvironmentId == "Environment-1" && r.ReleaseId == "Release-2");
        result.KeptReleases.Should().Contain(r => 
            r.ProjectId == "Project-1" && r.EnvironmentId == "Environment-2" && r.ReleaseId == "Release-1");
        result.KeptReleases.Should().Contain(r => 
            r.ProjectId == "Project-2" && r.EnvironmentId == "Environment-1" && r.ReleaseId == "Release-4");
    }

    #endregion

    #region Contract: Serializable DTOs

    [Fact]
    public void RetentionResult_IsJsonSerializable()
    {
        var projects = new[] { new Project("P1", "Project 1") };
        var environments = new[] { new Environment("E1", "Env 1") };
        var releases = new[] { new Release("R1", "P1", "1.0.0", Date(2000, 1, 1)) };
        var deployments = new[]
        {
            new Deployment("D1", "R1", "E1", Date(2000, 1, 2)),
            new Deployment("D-INVALID", "R-MISSING", "E1", Date(2000, 1, 3))
        };

        var result = _service.EvaluateRetention(
            projects, environments, releases, deployments,
            releasesToKeep: 1,
            correlationId: "test-123");

        var json = JsonSerializer.Serialize(result, new JsonSerializerOptions { WriteIndented = true });
        json.Should().NotBeNullOrEmpty();
        
        // Verify it can be parsed back (basic structure)
        var parsed = JsonSerializer.Deserialize<JsonElement>(json);
        parsed.TryGetProperty("KeptReleases", out _).Should().BeTrue();
        parsed.TryGetProperty("Decisions", out _).Should().BeTrue();
        parsed.TryGetProperty("Diagnostics", out _).Should().BeTrue();
    }

    #endregion
}
