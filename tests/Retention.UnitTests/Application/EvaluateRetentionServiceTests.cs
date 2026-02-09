using FluentAssertions;
using Retention.Application;
using Retention.Application.Errors;
using Retention.Application.Models;
using Retention.Domain.Entities;
using Retention.Domain.Services;
using Environment = Retention.Domain.Entities.Environment;

namespace Retention.UnitTests.Application;

public class EvaluateRetentionServiceTests
{
    private readonly IEvaluateRetentionService _service = new EvaluateRetentionService(
        new RetentionPolicyEvaluator(
            new DefaultGroupRetentionEvaluator(new DefaultRankingStrategy(), new TopNSelectionStrategy())));

    #region Test Helpers

    private static DateTimeOffset Date(int year, int month, int day, int hour = 0)
        => new(year, month, day, hour, 0, 0, TimeSpan.Zero);

    #endregion

    #region REQ-0009: Validation n >= 0

    [Fact]
    public void NegativeN_ThrowsValidationException_WithCorrectCode()
    {
        var projects = new[] { new Project("P1", "Project 1") };
        var environments = new[] { new Environment("E1", "Env 1") };
        var releases = new[] { new Release("R1", "P1", "1.0", Date(2000, 1, 1)) };
        var deployments = new[] { new Deployment("D1", "R1", "E1", Date(2000, 1, 2)) };

        var act = () => _service.EvaluateRetention(
            projects, environments, releases, deployments,
            releasesToKeep: -1);

        act.Should().Throw<ValidationException>()
            .Which.Code.Should().Be(ErrorCodes.NNegative);
    }

    [Fact]
    public void ZeroN_ReturnsEmptyKeptReleases()
    {
        var projects = new[] { new Project("P1", "Project 1") };
        var environments = new[] { new Environment("E1", "Env 1") };
        var releases = new[] { new Release("R1", "P1", "1.0", Date(2000, 1, 1)) };
        var deployments = new[] { new Deployment("D1", "R1", "E1", Date(2000, 1, 2)) };

        var result = _service.EvaluateRetention(
            projects, environments, releases, deployments,
            releasesToKeep: 0);

        result.KeptReleases.Should().BeEmpty();
        result.Diagnostics.TotalKeptReleases.Should().Be(0);
    }

    #endregion

    #region Null element validation

    [Fact]
    public void NullElementInProjects_ThrowsValidationException()
    {
        var projects = new Project?[] { new Project("P1", "Project 1"), null };
        var environments = new[] { new Environment("E1", "Env 1") };
        var releases = Array.Empty<Release>();
        var deployments = Array.Empty<Deployment>();

        var act = () => _service.EvaluateRetention(
            projects!, environments, releases, deployments,
            releasesToKeep: 1);

        act.Should().Throw<ValidationException>()
            .Which.Code.Should().Be(ErrorCodes.NullElement);
    }

    [Fact]
    public void NullElementInReleases_ThrowsValidationException()
    {
        var projects = new[] { new Project("P1", "Project 1") };
        var environments = new[] { new Environment("E1", "Env 1") };
        var releases = new Release?[] { null };
        var deployments = Array.Empty<Deployment>();

        var act = () => _service.EvaluateRetention(
            projects, environments, releases!, deployments,
            releasesToKeep: 1);

        act.Should().Throw<ValidationException>()
            .Which.Code.Should().Be(ErrorCodes.NullElement);
    }

    [Fact]
    public void NullCollections_TreatedAsEmpty()
    {
        var result = _service.EvaluateRetention(
            null, null, null, null,
            releasesToKeep: 1);

        result.KeptReleases.Should().BeEmpty();
        result.Decisions.Should().BeEmpty();
    }

    #endregion

    #region Duplicate ID validation

    [Fact]
    public void DuplicateProjectIds_ThrowsValidationException()
    {
        var projects = new[]
        {
            new Project("P1", "Project 1"),
            new Project("P1", "Project 1 Duplicate") // Duplicate ID
        };
        var environments = new[] { new Environment("E1", "Env 1") };
        var releases = Array.Empty<Release>();
        var deployments = Array.Empty<Deployment>();

        var act = () => _service.EvaluateRetention(
            projects, environments, releases, deployments,
            releasesToKeep: 1);

        act.Should().Throw<ValidationException>()
            .Which.Code.Should().Be(ErrorCodes.DuplicateProjectId);
    }

    [Fact]
    public void DuplicateEnvironmentIds_ThrowsValidationException()
    {
        var projects = new[] { new Project("P1", "Project 1") };
        var environments = new[]
        {
            new Environment("E1", "Env 1"),
            new Environment("E1", "Env 1 Duplicate") // Duplicate ID
        };
        var releases = Array.Empty<Release>();
        var deployments = Array.Empty<Deployment>();

        var act = () => _service.EvaluateRetention(
            projects, environments, releases, deployments,
            releasesToKeep: 1);

        act.Should().Throw<ValidationException>()
            .Which.Code.Should().Be(ErrorCodes.DuplicateEnvironmentId);
    }

    [Fact]
    public void DuplicateReleaseIds_ThrowsValidationException()
    {
        var projects = new[] { new Project("P1", "Project 1") };
        var environments = new[] { new Environment("E1", "Env 1") };
        var releases = new[]
        {
            new Release("R1", "P1", "1.0", Date(2000, 1, 1)),
            new Release("R1", "P1", "1.0", Date(2000, 1, 2)) // Duplicate ID
        };
        var deployments = Array.Empty<Deployment>();

        var act = () => _service.EvaluateRetention(
            projects, environments, releases, deployments,
            releasesToKeep: 1);

        act.Should().Throw<ValidationException>()
            .Which.Code.Should().Be(ErrorCodes.DuplicateReleaseId);
    }

    [Fact]
    public void DuplicateIds_ErrorMessageContainsDuplicateId()
    {
        var projects = new[]
        {
            new Project("P1", "Project 1"),
            new Project("P1", "Project 1 Duplicate")
        };
        var environments = new[] { new Environment("E1", "Env 1") };
        var releases = Array.Empty<Release>();
        var deployments = Array.Empty<Deployment>();

        var act = () => _service.EvaluateRetention(
            projects, environments, releases, deployments,
            releasesToKeep: 1);

        act.Should().Throw<ValidationException>()
            .Which.Message.Should().Contain("P1");
    }

    #endregion

    #region REQ-0010: Invalid reference handling

    [Fact]
    public void DeploymentWithMissingEnvironment_IsExcludedWithDiagnostic()
    {
        var projects = new[] { new Project("P1", "Project 1") };
        var environments = new[] { new Environment("E1", "Env 1") };
        var releases = new[] { new Release("R1", "P1", "1.0", Date(2000, 1, 1)) };
        var deployments = new[]
        {
            new Deployment("D1", "R1", "E1", Date(2000, 1, 2)),
            new Deployment("D2", "R1", "E-MISSING", Date(2000, 1, 3)) // Invalid environment
        };

        var result = _service.EvaluateRetention(
            projects, environments, releases, deployments,
            releasesToKeep: 1);

        // R1 should still be kept for E1
        result.KeptReleases.Should().HaveCount(1);
        result.KeptReleases[0].ReleaseId.Should().Be("R1");
        result.KeptReleases[0].EnvironmentId.Should().Be("E1");

        // Diagnostic should be emitted for the invalid deployment
        result.Diagnostics.InvalidDeploymentsExcluded.Should().Be(1);
        
        var diagnostic = result.Decisions.SingleOrDefault(d => d.ReasonCode == DecisionReasonCodes.InvalidReference);
        diagnostic.Should().NotBeNull();
        diagnostic!.ReasonText.Should().Contain("E-MISSING");
    }

    [Fact]
    public void DeploymentWithMissingRelease_IsExcludedWithDiagnostic()
    {
        var projects = new[] { new Project("P1", "Project 1") };
        var environments = new[] { new Environment("E1", "Env 1") };
        var releases = new[] { new Release("R1", "P1", "1.0", Date(2000, 1, 1)) };
        var deployments = new[]
        {
            new Deployment("D1", "R1", "E1", Date(2000, 1, 2)),
            new Deployment("D2", "R-MISSING", "E1", Date(2000, 1, 3)) // Invalid release
        };

        var result = _service.EvaluateRetention(
            projects, environments, releases, deployments,
            releasesToKeep: 1);

        result.KeptReleases.Should().HaveCount(1);
        result.Diagnostics.InvalidDeploymentsExcluded.Should().Be(1);
        
        var diagnostic = result.Decisions.SingleOrDefault(d => d.ReasonCode == DecisionReasonCodes.InvalidReference);
        diagnostic.Should().NotBeNull();
        diagnostic!.ReasonText.Should().Contain("R-MISSING");
    }

    [Fact]
    public void DeploymentWithMissingProject_IsExcludedWithDiagnostic()
    {
        var projects = new[] { new Project("P1", "Project 1") };
        var environments = new[] { new Environment("E1", "Env 1") };
        var releases = new[]
        {
            new Release("R1", "P1", "1.0", Date(2000, 1, 1)),
            new Release("R2", "P-MISSING", "1.0", Date(2000, 1, 1)) // Points to missing project
        };
        var deployments = new[]
        {
            new Deployment("D1", "R1", "E1", Date(2000, 1, 2)),
            new Deployment("D2", "R2", "E1", Date(2000, 1, 3)) // Invalid project reference
        };

        var result = _service.EvaluateRetention(
            projects, environments, releases, deployments,
            releasesToKeep: 1);

        result.KeptReleases.Should().HaveCount(1);
        result.KeptReleases[0].ReleaseId.Should().Be("R1");
        result.Diagnostics.InvalidDeploymentsExcluded.Should().Be(1);
    }

    [Fact]
    public void InvalidDeployments_DoNotAffectValidKeptSet()
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
            new Deployment("D3", "R-INVALID", "E1", Date(2000, 1, 5)) // Would be most recent if valid
        };

        var result = _service.EvaluateRetention(
            projects, environments, releases, deployments,
            releasesToKeep: 1);

        // R2 should be kept (most recent valid deployment)
        result.KeptReleases.Should().HaveCount(1);
        result.KeptReleases[0].ReleaseId.Should().Be("R2");
    }

    #endregion

    #region Decision log structure

    [Fact]
    public void DecisionLog_ContainsCorrectFields()
    {
        var projects = new[] { new Project("P1", "Project 1") };
        var environments = new[] { new Environment("E1", "Env 1") };
        var releases = new[] { new Release("R1", "P1", "1.0.0", Date(2000, 1, 1, 8)) };
        var deployments = new[] { new Deployment("D1", "R1", "E1", Date(2000, 1, 1, 10)) };

        var result = _service.EvaluateRetention(
            projects, environments, releases, deployments,
            releasesToKeep: 2,
            correlationId: "test-correlation-123");

        result.Decisions.Should().HaveCount(1);
        var decision = result.Decisions[0];
        
        decision.ProjectId.Should().Be("P1");
        decision.EnvironmentId.Should().Be("E1");
        decision.ReleaseId.Should().Be("R1");
        decision.N.Should().Be(2);
        decision.Rank.Should().Be(1);
        decision.LatestDeployedAt.Should().Be(Date(2000, 1, 1, 10));
        decision.ReasonCode.Should().Be(DecisionReasonCodes.KeptTopN);
        decision.CorrelationId.Should().Be("test-correlation-123");
    }

    [Fact]
    public void DecisionLog_KeptBeforeDiagnostic()
    {
        var projects = new[] { new Project("P1", "Project 1") };
        var environments = new[] { new Environment("E1", "Env 1") };
        var releases = new[] { new Release("R1", "P1", "1.0", Date(2000, 1, 1)) };
        var deployments = new[]
        {
            new Deployment("D1", "R1", "E1", Date(2000, 1, 2)),
            new Deployment("D2", "R-INVALID", "E1", Date(2000, 1, 3))
        };

        var result = _service.EvaluateRetention(
            projects, environments, releases, deployments,
            releasesToKeep: 1);

        result.Decisions.Should().HaveCount(2);
        result.Decisions[0].ReasonCode.Should().Be(DecisionReasonCodes.KeptTopN, "kept entries come first");
        result.Decisions[1].ReasonCode.Should().Be(DecisionReasonCodes.InvalidReference, "diagnostic entries come after");
    }

    #endregion

    #region KeptRelease DTO structure

    [Fact]
    public void KeptRelease_ContainsAllExpectedFields()
    {
        var projects = new[] { new Project("Project-1", "Test Project") };
        var environments = new[] { new Environment("Environment-1", "Test Env") };
        var releases = new[] { new Release("Release-1", "Project-1", "1.0.0", Date(2000, 1, 1, 8)) };
        var deployments = new[] { new Deployment("Deployment-1", "Release-1", "Environment-1", Date(2000, 1, 1, 10)) };

        var result = _service.EvaluateRetention(
            projects, environments, releases, deployments,
            releasesToKeep: 1);

        result.KeptReleases.Should().HaveCount(1);
        var kept = result.KeptReleases[0];
        
        kept.ReleaseId.Should().Be("Release-1");
        kept.ProjectId.Should().Be("Project-1");
        kept.EnvironmentId.Should().Be("Environment-1");
        kept.Version.Should().Be("1.0.0");
        kept.Created.Should().Be(Date(2000, 1, 1, 8));
        kept.LatestDeployedAt.Should().Be(Date(2000, 1, 1, 10));
        kept.Rank.Should().Be(1);
        kept.ReasonCode.Should().Be(DecisionReasonCodes.KeptTopN);
    }

    #endregion

    #region Diagnostics structure

    [Fact]
    public void Diagnostics_ReturnsCorrectCounts()
    {
        var projects = new[] { new Project("P1", "Project 1"), new Project("P2", "Project 2") };
        var environments = new[] { new Environment("E1", "Env 1"), new Environment("E2", "Env 2") };
        var releases = new[]
        {
            new Release("R1", "P1", "1.0", Date(2000, 1, 1)),
            new Release("R2", "P2", "1.0", Date(2000, 1, 1))
        };
        var deployments = new[]
        {
            new Deployment("D1", "R1", "E1", Date(2000, 1, 2)),
            new Deployment("D2", "R1", "E2", Date(2000, 1, 3)),
            new Deployment("D3", "R2", "E1", Date(2000, 1, 4)),
            new Deployment("D-INVALID", "R-MISSING", "E1", Date(2000, 1, 5))
        };

        var result = _service.EvaluateRetention(
            projects, environments, releases, deployments,
            releasesToKeep: 2);

        // 3 groups: (P1, E1), (P1, E2), (P2, E1)
        result.Diagnostics.GroupsEvaluated.Should().Be(3);
        result.Diagnostics.InvalidDeploymentsExcluded.Should().Be(1);
        result.Diagnostics.TotalKeptReleases.Should().Be(3); // One kept per group
    }

    #endregion

    #region Deterministic output ordering

    [Fact]
    public void KeptReleases_OrderedByProjectId_EnvironmentId_Rank_ReleaseId()
    {
        var projects = new[] { new Project("P-B", "Project B"), new Project("P-A", "Project A") };
        var environments = new[] { new Environment("E-2", "Env 2"), new Environment("E-1", "Env 1") };
        var releases = new[]
        {
            new Release("R1", "P-A", "1.0", Date(2000, 1, 1)),
            new Release("R2", "P-B", "1.0", Date(2000, 1, 2))
        };
        var deployments = new[]
        {
            new Deployment("D1", "R1", "E-1", Date(2000, 1, 3)),
            new Deployment("D2", "R2", "E-2", Date(2000, 1, 4))
        };

        var result = _service.EvaluateRetention(
            projects, environments, releases, deployments,
            releasesToKeep: 1);

        result.KeptReleases.Should().HaveCount(2);
        result.KeptReleases[0].ProjectId.Should().Be("P-A", "P-A < P-B alphabetically");
        result.KeptReleases[1].ProjectId.Should().Be("P-B");
    }

    [Fact]
    public void CorrelationId_NotGenerated_WhenNotProvided()
    {
        var projects = new[] { new Project("P1", "Project 1") };
        var environments = new[] { new Environment("E1", "Env 1") };
        var releases = new[] { new Release("R1", "P1", "1.0", Date(2000, 1, 1)) };
        var deployments = new[] { new Deployment("D1", "R1", "E1", Date(2000, 1, 2)) };

        var result = _service.EvaluateRetention(
            projects, environments, releases, deployments,
            releasesToKeep: 1,
            correlationId: null);

        result.Decisions.Should().AllSatisfy(d => d.CorrelationId.Should().BeNull());
    }

    #endregion
}
