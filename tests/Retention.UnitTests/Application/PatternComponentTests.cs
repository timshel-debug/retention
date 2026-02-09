using FluentAssertions;
using Retention.Application.Indexing;
using Retention.Application.Mapping;
using Retention.Application.Specifications;
using Retention.Application.Validation;
using Retention.Application.Validation.Rules;
using Retention.Application.Errors;
using Retention.Application.Evaluation;
using Retention.Application.Evaluation.Steps;
using Retention.Domain.Entities;
using Retention.Domain.Models;
using Retention.Domain.Services;
using Environment = Retention.Domain.Entities.Environment;

namespace Retention.UnitTests.Application;

/// <summary>
/// Tests for individual refactored pattern components.
/// </summary>
public class PatternComponentTests
{
    private static DateTimeOffset Date(int year, int month, int day, int hour = 0)
        => new(year, month, day, hour, 0, 0, TimeSpan.Zero);

    // ─────────────────────────────────────────────
    // Pattern 09: ReferenceIndex Builder
    // ─────────────────────────────────────────────

    [Fact]
    public void ReferenceIndexBuilder_BuildsCorrectDictionaries()
    {
        var builder = new ReferenceIndexBuilder();
        var projects = new[] { new Project("P1", "Proj1"), new Project("P2", "Proj2") };
        var environments = new[] { new Environment("E1", "Env1") };
        var releases = new[] { new Release("R1", "P1", "1.0", Date(2000, 1, 1)) };

        var index = builder.Build(projects, environments, releases);

        index.ProjectsById.Should().HaveCount(2);
        index.ProjectsById["P1"].Name.Should().Be("Proj1");
        index.EnvironmentsById.Should().HaveCount(1);
        index.ReleasesById.Should().HaveCount(1);
    }

    [Fact]
    public void ReferenceIndexBuilder_UsesOrdinalComparison()
    {
        var builder = new ReferenceIndexBuilder();
        var projects = new[] { new Project("abc", "lower"), new Project("ABC", "upper") };

        var index = builder.Build(projects, Array.Empty<Environment>(), Array.Empty<Release>());

        // Ordinal comparison: "abc" != "ABC"
        index.ProjectsById.Should().HaveCount(2);
    }

    // ─────────────────────────────────────────────
    // Pattern 03: Deployment Validity Specification
    // ─────────────────────────────────────────────

    [Fact]
    public void DeploymentValiditySpec_ValidDeployment_ReturnsValid()
    {
        var spec = new DefaultDeploymentValiditySpecification();
        var builder = new ReferenceIndexBuilder();
        var index = builder.Build(
            new[] { new Project("P1", "Proj") },
            new[] { new Environment("E1", "Env") },
            new[] { new Release("R1", "P1", "1.0", Date(2000, 1, 1)) });
        var deployment = new Deployment("D1", "R1", "E1", Date(2000, 1, 2));

        var result = spec.Evaluate(deployment, index);

        result.IsValid.Should().BeTrue();
        result.Reasons.Should().BeEmpty();
    }

    [Fact]
    public void DeploymentValiditySpec_MissingRelease_ReturnsInvalid()
    {
        var spec = new DefaultDeploymentValiditySpecification();
        var builder = new ReferenceIndexBuilder();
        var index = builder.Build(
            new[] { new Project("P1", "Proj") },
            new[] { new Environment("E1", "Env") },
            Array.Empty<Release>());
        var deployment = new Deployment("D1", "R-MISSING", "E1", Date(2000, 1, 2));

        var result = spec.Evaluate(deployment, index);

        result.IsValid.Should().BeFalse();
        result.Reasons.Should().Contain(r => r.Contains("release 'R-MISSING' not found"));
    }

    [Fact]
    public void DeploymentValiditySpec_MissingProject_ReturnsInvalid()
    {
        var spec = new DefaultDeploymentValiditySpecification();
        var builder = new ReferenceIndexBuilder();
        var index = builder.Build(
            Array.Empty<Project>(),
            new[] { new Environment("E1", "Env") },
            new[] { new Release("R1", "P-MISSING", "1.0", Date(2000, 1, 1)) });
        var deployment = new Deployment("D1", "R1", "E1", Date(2000, 1, 2));

        var result = spec.Evaluate(deployment, index);

        result.IsValid.Should().BeFalse();
        result.Reasons.Should().Contain(r => r.Contains("project 'P-MISSING' not found"));
    }

    [Fact]
    public void DeploymentValiditySpec_MissingEnvironment_ReturnsInvalid()
    {
        var spec = new DefaultDeploymentValiditySpecification();
        var builder = new ReferenceIndexBuilder();
        var index = builder.Build(
            new[] { new Project("P1", "Proj") },
            Array.Empty<Environment>(),
            new[] { new Release("R1", "P1", "1.0", Date(2000, 1, 1)) });
        var deployment = new Deployment("D1", "R1", "E-MISSING", Date(2000, 1, 2));

        var result = spec.Evaluate(deployment, index);

        result.IsValid.Should().BeFalse();
        result.Reasons.Should().Contain(r => r.Contains("environment 'E-MISSING' not found"));
    }

    [Fact]
    public void DeploymentValiditySpec_MultipleInvalid_ReturnsAllReasons()
    {
        var spec = new DefaultDeploymentValiditySpecification();
        var builder = new ReferenceIndexBuilder();
        var index = builder.Build(
            Array.Empty<Project>(),
            Array.Empty<Environment>(),
            Array.Empty<Release>());
        var deployment = new Deployment("D1", "R-X", "E-X", Date(2000, 1, 2));

        var result = spec.Evaluate(deployment, index);

        result.IsValid.Should().BeFalse();
        result.Reasons.Should().HaveCount(2);
    }

    // ─────────────────────────────────────────────
    // Pattern 04: Result Object / Notification (Diagnostics Accumulation)
    // ─────────────────────────────────────────────

    [Fact]
    public void FilterInvalidDeploymentsStep_ValidDeployments_ReturnsInResultObject()
    {
        var spec = new DefaultDeploymentValiditySpecification();
        var assembler = new DecisionLogAssembler();
        var step = new FilterInvalidDeploymentsStep(spec, assembler);
        var builder = new ReferenceIndexBuilder();
        var index = builder.Build(
            new[] { new Project("P1", "Proj") },
            new[] { new Environment("E1", "Env") },
            new[] { new Release("R1", "P1", "1.0", Date(2000, 1, 1)) });
        var deployments = new[]
        {
            new Deployment("D1", "R1", "E1", Date(2000, 1, 2)),
            new Deployment("D2", "R1", "E1", Date(2000, 1, 3)),
        };

        var context = new RetentionEvaluationContext
        {
            ReferenceIndex = index,
            Deployments = deployments,
            ReleasesToKeep = 1,
            CorrelationId = "test-corr"
        };

        step.Execute(context);

        context.FilteredDeployments.Should().NotBeNull();
        context.FilteredDeployments!.ValidDeployments.Should().HaveCount(2);
        context.FilteredDeployments.DiagnosticEntries.Should().BeEmpty();
        context.FilteredDeployments.InvalidExcludedCount.Should().Be(0);
    }

    [Fact]
    public void FilterInvalidDeploymentsStep_InvalidDeployments_ExcludesAndRecordsDiagnostics()
    {
        var spec = new DefaultDeploymentValiditySpecification();
        var assembler = new DecisionLogAssembler();
        var step = new FilterInvalidDeploymentsStep(spec, assembler);
        var builder = new ReferenceIndexBuilder();
        var index = builder.Build(
            new[] { new Project("P1", "Proj") },
            new[] { new Environment("E1", "Env") },
            new[] { new Release("R1", "P1", "1.0", Date(2000, 1, 1)) });
        var deployments = new[]
        {
            new Deployment("D1", "R1", "E1", Date(2000, 1, 2)),          // Valid
            new Deployment("D2", "R-MISSING", "E1", Date(2000, 1, 3)),   // Invalid
            new Deployment("D3", "R1", "E-MISSING", Date(2000, 1, 4)),   // Invalid
        };

        var context = new RetentionEvaluationContext
        {
            ReferenceIndex = index,
            Deployments = deployments,
            ReleasesToKeep = 1,
            CorrelationId = "test-corr"
        };

        step.Execute(context);

        context.FilteredDeployments.Should().NotBeNull();
        context.FilteredDeployments!.ValidDeployments.Should().HaveCount(1);
        context.FilteredDeployments.ValidDeployments[0].Id.Should().Be("D1");
        context.FilteredDeployments.DiagnosticEntries.Should().HaveCount(2);
        context.FilteredDeployments.InvalidExcludedCount.Should().Be(2);
    }

    [Fact]
    public void FilterInvalidDeploymentsStep_InvalidDeployment_ProjectIdFromRelease()
    {
        var spec = new DefaultDeploymentValiditySpecification();
        var assembler = new DecisionLogAssembler();
        var step = new FilterInvalidDeploymentsStep(spec, assembler);
        var builder = new ReferenceIndexBuilder();
        var index = builder.Build(
            new[] { new Project("P1", "Proj") },
            Array.Empty<Environment>(), // No environments
            new[] { new Release("R1", "P1", "1.0", Date(2000, 1, 1)) });
        var deployments = new[] { new Deployment("D1", "R1", "E-MISSING", Date(2000, 1, 2)) };

        var context = new RetentionEvaluationContext
        {
            ReferenceIndex = index,
            Deployments = deployments,
            ReleasesToKeep = 1,
            CorrelationId = "test-corr"
        };

        step.Execute(context);

        context.FilteredDeployments!.DiagnosticEntries.Should().HaveCount(1);
        context.FilteredDeployments.DiagnosticEntries[0].ProjectId.Should().Be("P1"); // From release
    }

    [Fact]
    public void FilterInvalidDeploymentsStep_InvalidDeployment_ProjectIdUnknownWhenReleaseNotFound()
    {
        var spec = new DefaultDeploymentValiditySpecification();
        var assembler = new DecisionLogAssembler();
        var step = new FilterInvalidDeploymentsStep(spec, assembler);
        var builder = new ReferenceIndexBuilder();
        var index = builder.Build(
            new[] { new Project("P1", "Proj") },
            new[] { new Environment("E1", "Env") },
            Array.Empty<Release>()); // No releases
        var deployments = new[] { new Deployment("D1", "R-MISSING", "E1", Date(2000, 1, 2)) };

        var context = new RetentionEvaluationContext
        {
            ReferenceIndex = index,
            Deployments = deployments,
            ReleasesToKeep = 1,
            CorrelationId = "test-corr"
        };

        step.Execute(context);

        context.FilteredDeployments!.DiagnosticEntries.Should().HaveCount(1);
        context.FilteredDeployments.DiagnosticEntries[0].ProjectId.Should().Be("unknown");
    }

    [Fact]
    public void FilterInvalidDeploymentsStep_ResultObject_MatchesDiagnosticRequirements()
    {
        // DIAG-REQ-0001: Filtering MUST not throw for invalid references
        // DIAG-REQ-0002: Diagnostic entry fields MUST match behavior
        var spec = new DefaultDeploymentValiditySpecification();
        var assembler = new DecisionLogAssembler();
        var step = new FilterInvalidDeploymentsStep(spec, assembler);
        var builder = new ReferenceIndexBuilder();
        var index = builder.Build(
            new[] { new Project("P1", "Proj") },
            new[] { new Environment("E1", "Env") },
            new[] { new Release("R1", "P1", "1.0", Date(2000, 1, 1)) });
        var deployments = new[]
        {
            new Deployment("D1", "R-X", "E1", Date(2000, 1, 2)),
        };

        var context = new RetentionEvaluationContext
        {
            ReferenceIndex = index,
            Deployments = deployments,
            ReleasesToKeep = 5,
            CorrelationId = "test-xyz"
        };

        // Should not throw (DIAG-REQ-0001)
        var act = () => step.Execute(context);
        act.Should().NotThrow();

        // Verify diagnostic fields (DIAG-REQ-0002)
        var entry = context.FilteredDeployments!.DiagnosticEntries[0];
        entry.ProjectId.Should().Be("unknown");
        entry.EnvironmentId.Should().Be("E1");
        entry.ReleaseId.Should().Be("R-X");
        entry.Rank.Should().Be(0);
        entry.LatestDeployedAt.Should().BeNull();
        entry.ReasonCode.Should().Contain("invalid");
        entry.CorrelationId.Should().Be("test-xyz");
    }

    // ─────────────────────────────────────────────
    // Pattern 02: Validation Rules
    // ─────────────────────────────────────────────

    [Fact]
    public void ValidationChain_NegativeReleasesToKeep_ThrowsWithCorrectCode()
    {
        var rules = ValidationRuleChainFactory.CreateDefaultChain();
        var ctx = new ValidationContext(
            new[] { new Project("P1", "Proj") },
            new[] { new Environment("E1", "Env") },
            Array.Empty<Release>(),
            Array.Empty<Deployment>(),
            releasesToKeep: -1);

        var act = () =>
        {
            foreach (var rule in rules) rule.Validate(ctx);
        };

        act.Should().Throw<ValidationException>()
            .Which.Code.Should().Be(ErrorCodes.NNegative);
    }

    [Fact]
    public void ValidationChain_NullElement_ThrowsWithCorrectCode()
    {
        var rules = ValidationRuleChainFactory.CreateDefaultChain();
        var ctx = new ValidationContext(
            new Project?[] { null }!,
            Array.Empty<Environment>(),
            Array.Empty<Release>(),
            Array.Empty<Deployment>(),
            releasesToKeep: 1);

        var act = () =>
        {
            foreach (var rule in rules) rule.Validate(ctx);
        };

        act.Should().Throw<ValidationException>()
            .Which.Code.Should().Be(ErrorCodes.NullElement);
    }

    [Fact]
    public void ValidationChain_DuplicateProjectId_ThrowsWithCorrectCode()
    {
        var rules = ValidationRuleChainFactory.CreateDefaultChain();
        var ctx = new ValidationContext(
            new[] { new Project("P1", "A"), new Project("P1", "B") },
            Array.Empty<Environment>(),
            Array.Empty<Release>(),
            Array.Empty<Deployment>(),
            releasesToKeep: 1);

        var act = () =>
        {
            foreach (var rule in rules) rule.Validate(ctx);
        };

        act.Should().Throw<ValidationException>()
            .Which.Code.Should().Be(ErrorCodes.DuplicateProjectId);
    }

    [Fact]
    public void ValidationChain_ValidInputs_DoesNotThrow()
    {
        var rules = ValidationRuleChainFactory.CreateDefaultChain();
        var ctx = new ValidationContext(
            new[] { new Project("P1", "Proj") },
            new[] { new Environment("E1", "Env") },
            new[] { new Release("R1", "P1", "1.0", Date(2000, 1, 1)) },
            new[] { new Deployment("D1", "R1", "E1", Date(2000, 1, 2)) },
            releasesToKeep: 1);

        var act = () =>
        {
            foreach (var rule in rules) rule.Validate(ctx);
        };

        act.Should().NotThrow();
    }

    // ─────────────────────────────────────────────
    // Pattern 07: Mapper + Assembler
    // ─────────────────────────────────────────────

    [Fact]
    public void KeptReleaseMapper_MapsAllFields()
    {
        var mapper = new KeptReleaseMapper();
        var candidate = new ReleaseCandidate("P1", "E1", "R1", "1.0", Date(2000, 1, 1), Date(2000, 1, 2), 1, ReasonCodes.KeptTopN);

        var result = mapper.Map(candidate);

        result.ReleaseId.Should().Be("R1");
        result.ProjectId.Should().Be("P1");
        result.EnvironmentId.Should().Be("E1");
        result.Version.Should().Be("1.0");
        result.Created.Should().Be(Date(2000, 1, 1));
        result.LatestDeployedAt.Should().Be(Date(2000, 1, 2));
        result.Rank.Should().Be(1);
        result.ReasonCode.Should().Be(ReasonCodes.KeptTopN);
    }

    [Fact]
    public void DecisionLogAssembler_KeptEntry_HasExactFormat()
    {
        var assembler = new DecisionLogAssembler();
        var candidate = new ReleaseCandidate("P1", "E1", "R1", "1.0", Date(2000, 1, 1), Date(2000, 1, 2), 2, ReasonCodes.KeptTopN);

        var entry = assembler.BuildKeptEntry(candidate, releasesToKeep: 3, correlationId: "corr-1");

        entry.ReasonText.Should().Be("Release 'R1' kept: rank 2 of 3 for project 'P1' / environment 'E1'");
        entry.ReasonCode.Should().Be("kept.top_n");
        entry.CorrelationId.Should().Be("corr-1");
    }

    [Fact]
    public void DecisionLogAssembler_InvalidEntry_HasExactFormat()
    {
        var assembler = new DecisionLogAssembler();
        var deployment = new Deployment("D1", "R-X", "E1", Date(2000, 1, 2));
        var reasons = new[] { "release 'R-X' not found" };

        var entry = assembler.BuildInvalidDeploymentEntry(deployment, "unknown", 5, reasons, "corr-2");

        entry.ReasonText.Should().Be("Deployment 'D1' excluded: release 'R-X' not found");
        entry.ProjectId.Should().Be("unknown");
        entry.Rank.Should().Be(0);
        entry.LatestDeployedAt.Should().BeNull();
        entry.CorrelationId.Should().Be("corr-2");
    }

    [Fact]
    public void DiagnosticsCalculator_ComputesCorrectCounts()
    {
        var calc = new DiagnosticsCalculator();
        var candidates = new[]
        {
            new ReleaseCandidate("P1", "E1", "R1", "1.0", Date(2000, 1, 1), Date(2000, 1, 2), 1, ReasonCodes.KeptTopN),
            new ReleaseCandidate("P1", "E2", "R2", "2.0", Date(2000, 1, 1), Date(2000, 1, 3), 1, ReasonCodes.KeptTopN),
        };
        var keptReleases = new[] { new Retention.Application.Models.KeptRelease("R1", "P1", "E1", "1.0", Date(2000, 1, 1), Date(2000, 1, 2), 1, ReasonCodes.KeptTopN) };

        var diag = calc.Calculate(candidates, invalidExcludedCount: 3, (IReadOnlyList<Retention.Application.Models.KeptRelease>)keptReleases);

        diag.GroupsEvaluated.Should().Be(2);
        diag.InvalidDeploymentsExcluded.Should().Be(3);
        diag.TotalKeptReleases.Should().Be(1);
    }

    // ─────────────────────────────────────────────
    // Pattern 05: Default Ranking Strategy
    // ─────────────────────────────────────────────

    [Fact]
    public void DefaultRankingStrategy_SortsByLatestDeployedAtDesc()
    {
        var strategy = new DefaultRankingStrategy();
        var entries = new[]
        {
            new GroupEntry("R1", "1.0", Date(2000, 1, 1), Date(2000, 1, 3)),
            new GroupEntry("R2", "2.0", Date(2000, 1, 2), Date(2000, 1, 5)),
            new GroupEntry("R3", "3.0", Date(2000, 1, 3), Date(2000, 1, 4)),
        };

        var ranked = strategy.Rank(entries);

        ranked[0].ReleaseId.Should().Be("R2"); // LatestDeployedAt = Jan 5
        ranked[1].ReleaseId.Should().Be("R3"); // LatestDeployedAt = Jan 4
        ranked[2].ReleaseId.Should().Be("R1"); // LatestDeployedAt = Jan 3
        ranked[0].Rank.Should().Be(1);
        ranked[1].Rank.Should().Be(2);
        ranked[2].Rank.Should().Be(3);
    }

    [Fact]
    public void DefaultRankingStrategy_TieBreaker_CreatedDescThenIdAsc()
    {
        var strategy = new DefaultRankingStrategy();
        var entries = new[]
        {
            new GroupEntry("R-B", "1.0", Date(2000, 1, 1, 8), Date(2000, 1, 2)),
            new GroupEntry("R-A", "2.0", Date(2000, 1, 1, 8), Date(2000, 1, 2)),
            new GroupEntry("R-C", "3.0", Date(2000, 1, 1, 10), Date(2000, 1, 2)), // Created later
        };

        var ranked = strategy.Rank(entries);

        // Same LatestDeployedAt → Created desc → R-C first
        // R-A and R-B same Created → Id asc → R-A before R-B
        ranked[0].ReleaseId.Should().Be("R-C");
        ranked[1].ReleaseId.Should().Be("R-A");
        ranked[2].ReleaseId.Should().Be("R-B");
    }

    [Fact]
    public void TopNSelectionStrategy_SelectsFirstN()
    {
        var strategy = new TopNSelectionStrategy();
        var ranked = new[]
        {
            new RankedCandidate("R1", "1.0", Date(2000, 1, 1), Date(2000, 1, 3), 1),
            new RankedCandidate("R2", "2.0", Date(2000, 1, 2), Date(2000, 1, 2), 2),
            new RankedCandidate("R3", "3.0", Date(2000, 1, 3), Date(2000, 1, 1), 3),
        };

        var selected = strategy.Select(ranked, releasesToKeep: 2);

        selected.Should().HaveCount(2);
        selected[0].ReleaseId.Should().Be("R1");
        selected[1].ReleaseId.Should().Be("R2");
    }

    // ─────────────────────────────────────────────
    // Pattern 06: Group Evaluator
    // ─────────────────────────────────────────────

    [Fact]
    public void DefaultGroupEvaluator_ProducesCorrectCandidates()
    {
        var evaluator = new DefaultGroupRetentionEvaluator(new DefaultRankingStrategy(), new TopNSelectionStrategy());
        var entries = new[]
        {
            new GroupEntry("R1", "1.0", Date(2000, 1, 1), Date(2000, 1, 3)),
            new GroupEntry("R2", "2.0", Date(2000, 1, 2), Date(2000, 1, 4)),
        };

        var result = evaluator.EvaluateGroup("P1", "E1", entries, releasesToKeep: 1);

        result.Should().HaveCount(1);
        result[0].ReleaseId.Should().Be("R2");
        result[0].ProjectId.Should().Be("P1");
        result[0].EnvironmentId.Should().Be("E1");
        result[0].Rank.Should().Be(1);
        result[0].ReasonCode.Should().Be(ReasonCodes.KeptTopN);
    }

    // ─────────────────────────────────────────────
    // Pattern 10: Engine (pure, no telemetry needed)
    // ─────────────────────────────────────────────

    [Fact]
    public void Engine_EvaluatesWithoutTelemetry()
    {
        var engine = Helpers.TestEngineFactory.CreateEngine();

        var inputs = new RetentionEvaluationInputs(
            new[] { new Project("P1", "Proj") },
            new[] { new Environment("E1", "Env") },
            new[] { new Release("R1", "P1", "1.0", Date(2000, 1, 1)) },
            new[] { new Deployment("D1", "R1", "E1", Date(2000, 1, 2)) },
            ReleasesToKeep: 1,
            CorrelationId: null);

        var result = engine.Evaluate(inputs);

        result.KeptReleases.Should().HaveCount(1);
        result.KeptReleases[0].ReleaseId.Should().Be("R1");
        result.Diagnostics.GroupsEvaluated.Should().Be(1);
    }

    [Fact]
    public void Engine_IsPureAndDeterministic()
    {
        var engine = Helpers.TestEngineFactory.CreateEngine();

        var inputs = new RetentionEvaluationInputs(
            new[] { new Project("P1", "Proj"), new Project("P2", "Proj2") },
            new[] { new Environment("E1", "Env") },
            new[]
            {
                new Release("R1", "P1", "1.0", Date(2000, 1, 1)),
                new Release("R2", "P2", "2.0", Date(2000, 1, 2)),
            },
            new[]
            {
                new Deployment("D1", "R1", "E1", Date(2000, 1, 3)),
                new Deployment("D2", "R2", "E1", Date(2000, 1, 4)),
            },
            ReleasesToKeep: 1,
            CorrelationId: "test");

        var result1 = engine.Evaluate(inputs);
        var result2 = engine.Evaluate(inputs);

        result1.Should().BeEquivalentTo(result2, opts => opts.WithStrictOrdering());
    }

    // ── Pattern 01 Pipeline Guards ────────────────────────────────

    [Fact]
    public void FilterInvalidDeploymentsStep_ThrowsWhenReferenceIndexMissing()
    {
        var spec = new DefaultDeploymentValiditySpecification();
        var assembler = new DecisionLogAssembler();
        var step = new FilterInvalidDeploymentsStep(spec, assembler);
        var context = new RetentionEvaluationContext
        {
            Deployments = Array.Empty<Deployment>(),
            ReferenceIndex = null,
        };

        var act = () => step.Execute(context);

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*ReferenceIndex*");
    }

    [Fact]
    public void EvaluatePolicyStep_ThrowsWhenFilteredDeploymentsMissing()
    {
        var evaluator = new RetentionPolicyEvaluator(
            new DefaultGroupRetentionEvaluator(new DefaultRankingStrategy(), new TopNSelectionStrategy()));
        var step = new EvaluatePolicyStep(evaluator);
        var context = new RetentionEvaluationContext
        {
            ReferenceIndex = new ReferenceIndex(
                new Dictionary<string, Project>(),
                new Dictionary<string, Environment>(),
                new Dictionary<string, Release>()),
            FilteredDeployments = null,
        };

        var act = () => step.Execute(context);

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*FilteredDeployments*");
    }
}
