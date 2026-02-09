using FluentAssertions;
using Retention.Application;
using Retention.Application.Models;
using Retention.Domain.Entities;
using Retention.UnitTests.Helpers;
using Environment = Retention.Domain.Entities.Environment;

namespace Retention.UnitTests.Application;

/// <summary>
/// Golden snapshot tests that capture exact pre-refactor outputs.
/// These tests verify that the refactored implementation produces
/// byte-for-byte identical results to the original.
/// </summary>
public class GoldenSnapshotTests
{
    private readonly IEvaluateRetentionService _service = TestEngineFactory.CreateService();

    private static DateTimeOffset Date(int year, int month, int day, int hour = 0)
        => new(year, month, day, hour, 0, 0, TimeSpan.Zero);

    // ───────────────────────────────────────────────────────────
    // Fixture A: No data (all nulls)
    // ───────────────────────────────────────────────────────────
    [Fact]
    public void GoldenA_NoData_NullInputs()
    {
        var result = _service.EvaluateRetention(null, null, null, null, releasesToKeep: 1);

        result.KeptReleases.Should().BeEmpty();
        result.Decisions.Should().BeEmpty();
        result.Diagnostics.GroupsEvaluated.Should().Be(0);
        result.Diagnostics.InvalidDeploymentsExcluded.Should().Be(0);
        result.Diagnostics.TotalKeptReleases.Should().Be(0);
    }

    [Fact]
    public void GoldenA_NoData_EmptyLists()
    {
        var result = _service.EvaluateRetention(
            Array.Empty<Project>(),
            Array.Empty<Environment>(),
            Array.Empty<Release>(),
            Array.Empty<Deployment>(),
            releasesToKeep: 5);

        result.KeptReleases.Should().BeEmpty();
        result.Decisions.Should().BeEmpty();
        result.Diagnostics.GroupsEvaluated.Should().Be(0);
        result.Diagnostics.InvalidDeploymentsExcluded.Should().Be(0);
        result.Diagnostics.TotalKeptReleases.Should().Be(0);
    }

    // ───────────────────────────────────────────────────────────
    // Fixture B: releasesToKeep = 0
    // ───────────────────────────────────────────────────────────
    [Fact]
    public void GoldenB_KeepZero_WithValidData()
    {
        var projects = new[] { new Project("P1", "Project 1") };
        var environments = new[] { new Environment("E1", "Env 1") };
        var releases = new[] { new Release("R1", "P1", "1.0", Date(2000, 1, 1)) };
        var deployments = new[] { new Deployment("D1", "R1", "E1", Date(2000, 1, 2)) };

        var result = _service.EvaluateRetention(
            projects, environments, releases, deployments, releasesToKeep: 0);

        result.KeptReleases.Should().BeEmpty();
        result.Diagnostics.TotalKeptReleases.Should().Be(0);
        result.Diagnostics.GroupsEvaluated.Should().Be(0);
        result.Diagnostics.InvalidDeploymentsExcluded.Should().Be(0);
        // With keep=0, domain returns empty, so no kept decisions; no invalid deployments either
        result.Decisions.Should().BeEmpty();
    }

    [Fact]
    public void GoldenB_KeepZero_WithInvalidDeployments()
    {
        var projects = new[] { new Project("P1", "Project 1") };
        var environments = new[] { new Environment("E1", "Env 1") };
        var releases = new[] { new Release("R1", "P1", "1.0", Date(2000, 1, 1)) };
        var deployments = new[]
        {
            new Deployment("D1", "R1", "E1", Date(2000, 1, 2)),
            new Deployment("D2", "R-MISSING", "E1", Date(2000, 1, 3))
        };

        var result = _service.EvaluateRetention(
            projects, environments, releases, deployments, releasesToKeep: 0);

        result.KeptReleases.Should().BeEmpty();
        result.Diagnostics.TotalKeptReleases.Should().Be(0);
        result.Diagnostics.InvalidDeploymentsExcluded.Should().Be(1);
        // Even with keep=0, diagnostic entries for invalid deployments should be present
        result.Decisions.Should().HaveCount(1);
        result.Decisions[0].ReasonCode.Should().Be(DecisionReasonCodes.InvalidReference);
    }

    // ───────────────────────────────────────────────────────────
    // Fixture C: Single group, multiple deployments, ties
    // ───────────────────────────────────────────────────────────
    [Fact]
    public void GoldenC_SingleGroup_MultipleDeploys_WithTies()
    {
        var projects = new[] { new Project("P1", "Project 1") };
        var environments = new[] { new Environment("E1", "Env 1") };
        var releases = new[]
        {
            new Release("R-A", "P1", "1.0", Date(2000, 1, 1, 8)),   // Created first
            new Release("R-B", "P1", "1.1", Date(2000, 1, 1, 8)),   // Same Created (tie on Created → id asc)
            new Release("R-C", "P1", "1.2", Date(2000, 1, 1, 10)),  // Created later
        };
        var deployments = new[]
        {
            // R-A and R-B deployed at same time, R-C deployed at same time too
            new Deployment("D1", "R-A", "E1", Date(2000, 1, 2, 12)),
            new Deployment("D2", "R-B", "E1", Date(2000, 1, 2, 12)),
            new Deployment("D3", "R-C", "E1", Date(2000, 1, 2, 12)),
        };

        var result = _service.EvaluateRetention(
            projects, environments, releases, deployments,
            releasesToKeep: 2, correlationId: "golden-c");

        // All three have same LatestDeployedAt.
        // Tie-break 1: Created desc → R-C (10:00) first
        // Tie-break 2: R-A and R-B both Created 08:00 → Id asc → R-A before R-B
        // Keep 2 → R-C (rank 1), R-A (rank 2)
        result.KeptReleases.Should().HaveCount(2);
        result.KeptReleases[0].ReleaseId.Should().Be("R-C");
        result.KeptReleases[0].Rank.Should().Be(1);
        result.KeptReleases[1].ReleaseId.Should().Be("R-A");
        result.KeptReleases[1].Rank.Should().Be(2);

        // Decision log: 2 kept entries, no diagnostics
        // Sorted by ProjectId asc, EnvironmentId asc, Rank asc, ReleaseId asc
        result.Decisions.Should().HaveCount(2);
        result.Decisions[0].ReleaseId.Should().Be("R-C");
        result.Decisions[0].Rank.Should().Be(1);
        result.Decisions[0].ReasonText.Should().Be("Release 'R-C' kept: rank 1 of 2 for project 'P1' / environment 'E1'");
        result.Decisions[0].CorrelationId.Should().Be("golden-c");
        result.Decisions[1].ReleaseId.Should().Be("R-A");
        result.Decisions[1].Rank.Should().Be(2);
        result.Decisions[1].ReasonText.Should().Be("Release 'R-A' kept: rank 2 of 2 for project 'P1' / environment 'E1'");
        result.Decisions[1].CorrelationId.Should().Be("golden-c");

        result.Diagnostics.GroupsEvaluated.Should().Be(1);
        result.Diagnostics.InvalidDeploymentsExcluded.Should().Be(0);
        result.Diagnostics.TotalKeptReleases.Should().Be(2);
    }

    // ───────────────────────────────────────────────────────────
    // Fixture D: Multiple groups
    // ───────────────────────────────────────────────────────────
    [Fact]
    public void GoldenD_MultipleGroups()
    {
        var projects = new[]
        {
            new Project("P1", "Project 1"),
            new Project("P2", "Project 2")
        };
        var environments = new[]
        {
            new Environment("E1", "Env 1"),
            new Environment("E2", "Env 2")
        };
        var releases = new[]
        {
            new Release("R1", "P1", "1.0", Date(2000, 1, 1)),
            new Release("R2", "P1", "1.1", Date(2000, 1, 2)),
            new Release("R3", "P2", "1.0", Date(2000, 1, 1)),
        };
        var deployments = new[]
        {
            new Deployment("D1", "R1", "E1", Date(2000, 1, 3)),
            new Deployment("D2", "R2", "E1", Date(2000, 1, 4)),
            new Deployment("D3", "R1", "E2", Date(2000, 1, 5)),
            new Deployment("D4", "R3", "E1", Date(2000, 1, 6)),
        };

        var result = _service.EvaluateRetention(
            projects, environments, releases, deployments, releasesToKeep: 1);

        // Groups: (P1,E1) → R2 rank 1; (P1,E2) → R1 rank 1; (P2,E1) → R3 rank 1
        result.KeptReleases.Should().HaveCount(3);
        // Ordering: ProjectId asc, then EnvironmentId asc
        result.KeptReleases[0].Should().Match<KeptRelease>(k =>
            k.ProjectId == "P1" && k.EnvironmentId == "E1" && k.ReleaseId == "R2" && k.Rank == 1);
        result.KeptReleases[1].Should().Match<KeptRelease>(k =>
            k.ProjectId == "P1" && k.EnvironmentId == "E2" && k.ReleaseId == "R1" && k.Rank == 1);
        result.KeptReleases[2].Should().Match<KeptRelease>(k =>
            k.ProjectId == "P2" && k.EnvironmentId == "E1" && k.ReleaseId == "R3" && k.Rank == 1);

        result.Diagnostics.GroupsEvaluated.Should().Be(3);
        result.Diagnostics.TotalKeptReleases.Should().Be(3);
        result.Diagnostics.InvalidDeploymentsExcluded.Should().Be(0);

        result.Decisions.Should().HaveCount(3);
        result.Decisions.Should().AllSatisfy(d =>
        {
            d.ReasonCode.Should().Be(DecisionReasonCodes.KeptTopN);
            d.DecisionType.Should().Be("kept");
        });
    }

    // ───────────────────────────────────────────────────────────
    // Fixture E: Invalid references (missing release, project, env)
    // ───────────────────────────────────────────────────────────
    [Fact]
    public void GoldenE_InvalidReferences_AllTypes()
    {
        var projects = new[] { new Project("P1", "Project 1") };
        var environments = new[] { new Environment("E1", "Env 1") };
        var releases = new[]
        {
            new Release("R1", "P1", "1.0", Date(2000, 1, 1)),
            new Release("R2", "P-MISSING", "2.0", Date(2000, 1, 2)), // release exists but project missing
        };
        var deployments = new[]
        {
            // Valid
            new Deployment("D1", "R1", "E1", Date(2000, 1, 3)),
            // Missing release
            new Deployment("D2", "R-GONE", "E1", Date(2000, 1, 4)),
            // Missing environment
            new Deployment("D3", "R1", "E-GONE", Date(2000, 1, 5)),
            // Release exists but its project is missing
            new Deployment("D4", "R2", "E1", Date(2000, 1, 6)),
            // Both missing release AND missing environment
            new Deployment("D5", "R-GONE2", "E-GONE2", Date(2000, 1, 7)),
        };

        var result = _service.EvaluateRetention(
            projects, environments, releases, deployments,
            releasesToKeep: 1, correlationId: "golden-e");

        // 1 valid kept release
        result.KeptReleases.Should().HaveCount(1);
        result.KeptReleases[0].ReleaseId.Should().Be("R1");

        // 4 invalid deployments
        result.Diagnostics.InvalidDeploymentsExcluded.Should().Be(4);

        // Decision ordering: kept first, then diagnostics
        result.Decisions.Should().HaveCount(5);
        result.Decisions[0].ReasonCode.Should().Be(DecisionReasonCodes.KeptTopN);

        var diagnostics = result.Decisions.Where(d => d.ReasonCode == DecisionReasonCodes.InvalidReference).ToList();
        diagnostics.Should().HaveCount(4);

        // Verify exact ReasonText format for missing release
        diagnostics.Should().Contain(d =>
            d.ReasonText == "Deployment 'D2' excluded: release 'R-GONE' not found" &&
            d.ProjectId == "unknown" &&
            d.ReleaseId == "R-GONE");

        // Verify exact ReasonText format for missing environment
        diagnostics.Should().Contain(d =>
            d.ReasonText == "Deployment 'D3' excluded: environment 'E-GONE' not found" &&
            d.ReleaseId == "R1");

        // Verify exact ReasonText format for missing project (release exists)
        diagnostics.Should().Contain(d =>
            d.ReasonText == "Deployment 'D4' excluded: project 'P-MISSING' not found" &&
            d.ProjectId == "P-MISSING");

        // Verify exact ReasonText format for both missing release AND environment
        diagnostics.Should().Contain(d =>
            d.ReasonText == "Deployment 'D5' excluded: release 'R-GONE2' not found; environment 'E-GONE2' not found" &&
            d.ProjectId == "unknown");

        // All diagnostics have correlation ID propagated
        diagnostics.Should().AllSatisfy(d => d.CorrelationId.Should().Be("golden-e"));
    }

    // ───────────────────────────────────────────────────────────
    // Fixture F: Decision ordering preserved exactly
    // ───────────────────────────────────────────────────────────
    [Fact]
    public void GoldenF_DecisionOrdering_KeptBeforeDiagnostic_ThenSorted()
    {
        var projects = new[]
        {
            new Project("P-B", "ProjB"),
            new Project("P-A", "ProjA")
        };
        var environments = new[]
        {
            new Environment("E-2", "Env2"),
            new Environment("E-1", "Env1")
        };
        var releases = new[]
        {
            new Release("R1", "P-A", "1.0", Date(2000, 1, 1)),
            new Release("R2", "P-B", "1.0", Date(2000, 1, 2)),
        };
        var deployments = new[]
        {
            new Deployment("D1", "R1", "E-1", Date(2000, 1, 3)),
            new Deployment("D2", "R2", "E-2", Date(2000, 1, 4)),
            new Deployment("D-BAD", "R-INVALID", "E-1", Date(2000, 1, 5)),
        };

        var result = _service.EvaluateRetention(
            projects, environments, releases, deployments, releasesToKeep: 1);

        result.Decisions.Should().HaveCount(3);
        // Kept entries first (sorted by ProjectId asc, EnvironmentId asc, Rank asc)
        result.Decisions[0].DecisionType.Should().Be("kept");
        result.Decisions[0].ProjectId.Should().Be("P-A");
        result.Decisions[1].DecisionType.Should().Be("kept");
        result.Decisions[1].ProjectId.Should().Be("P-B");
        // Diagnostic entries after
        result.Decisions[2].DecisionType.Should().Be("diagnostic");
    }

    // ───────────────────────────────────────────────────────────
    // Fixture G: ReasonText exact format verification
    // ───────────────────────────────────────────────────────────
    [Fact]
    public void GoldenG_ReasonText_ExactFormat()
    {
        var projects = new[] { new Project("P1", "Proj") };
        var environments = new[] { new Environment("E1", "Env") };
        var releases = new[]
        {
            new Release("R1", "P1", "1.0", Date(2000, 1, 1)),
            new Release("R2", "P1", "2.0", Date(2000, 1, 2)),
        };
        var deployments = new[]
        {
            new Deployment("D1", "R1", "E1", Date(2000, 1, 3)),
            new Deployment("D2", "R2", "E1", Date(2000, 1, 4)),
        };

        var result = _service.EvaluateRetention(
            projects, environments, releases, deployments, releasesToKeep: 2);

        result.Decisions.Should().HaveCount(2);
        // Decisions are sorted by ProjectId, EnvironmentId, Rank, ReleaseId
        // Both are in same project/env, so sorted by Rank
        var rank1 = result.Decisions.Single(d => d.Rank == 1);
        var rank2 = result.Decisions.Single(d => d.Rank == 2);

        rank1.ReasonText.Should().Be("Release 'R2' kept: rank 1 of 2 for project 'P1' / environment 'E1'");
        rank2.ReasonText.Should().Be("Release 'R1' kept: rank 2 of 2 for project 'P1' / environment 'E1'");
    }
}
