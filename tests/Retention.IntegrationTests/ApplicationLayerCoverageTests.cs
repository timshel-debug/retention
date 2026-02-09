using FluentAssertions;
using Retention.Application;
using Retention.Application.Errors;
using Retention.Application.Models;
using Retention.Domain.Entities;
using Retention.IntegrationTests.Helpers;
using Environment = Retention.Domain.Entities.Environment;

namespace Retention.IntegrationTests;

/// <summary>
/// Additional integration tests for Application layer edge cases and coverage gaps.
/// Tests validation logic, error handling, and complex scenarios.
/// </summary>
public class ApplicationLayerCoverageTests
{
    private readonly EvaluateRetentionService _service = TestEngineFactory.CreateService();

    [Fact]
    public void EvaluateRetention_WithNullProjects_TreatsAsEmpty()
    {
        // Arrange
        var environments = new[] { new Environment("E1", "Env 1") };
        var releases = new[] { new Release("R1", "P1", "1.0", DateTimeOffset.Parse("2024-01-01T10:00:00Z")) };
        var deployments = new[] { new Deployment("D1", "R1", "E1", DateTimeOffset.Parse("2024-01-01T11:00:00Z")) };

        // Act
        var result = _service.EvaluateRetention(null, environments, releases, deployments, 1);

        // Assert
        result.KeptReleases.Should().BeEmpty(); // No projects, so no valid releases
        result.Diagnostics.InvalidDeploymentsExcluded.Should().Be(1);
    }

    [Fact]
    public void EvaluateRetention_WithNullEnvironments_TreatsAsEmpty()
    {
        // Arrange
        var projects = new[] { new Project("P1", "Project 1") };
        var releases = new[] { new Release("R1", "P1", "1.0", DateTimeOffset.Parse("2024-01-01T10:00:00Z")) };
        var deployments = new[] { new Deployment("D1", "R1", "E1", DateTimeOffset.Parse("2024-01-01T11:00:00Z")) };

        // Act
        var result = _service.EvaluateRetention(projects, null, releases, deployments, 1);

        // Assert
        result.KeptReleases.Should().BeEmpty(); // No environments, so deployments invalid
        result.Diagnostics.InvalidDeploymentsExcluded.Should().Be(1);
    }

    [Fact]
    public void EvaluateRetention_WithNullReleases_TreatsAsEmpty()
    {
        // Arrange
        var projects = new[] { new Project("P1", "Project 1") };
        var environments = new[] { new Environment("E1", "Env 1") };
        var deployments = new[] { new Deployment("D1", "R1", "E1", DateTimeOffset.Parse("2024-01-01T11:00:00Z")) };

        // Act
        var result = _service.EvaluateRetention(projects, environments, null, deployments, 1);

        // Assert
        result.KeptReleases.Should().BeEmpty();
        result.Diagnostics.InvalidDeploymentsExcluded.Should().Be(1);
    }

    [Fact]
    public void EvaluateRetention_WithNullDeployments_ReturnsEmpty()
    {
        // Arrange
        var projects = new[] { new Project("P1", "Project 1") };
        var environments = new[] { new Environment("E1", "Env 1") };
        var releases = new[] { new Release("R1", "P1", "1.0", DateTimeOffset.Parse("2024-01-01T10:00:00Z")) };

        // Act
        var result = _service.EvaluateRetention(projects, environments, releases, null, 1);

        // Assert
        result.KeptReleases.Should().BeEmpty();
        result.Diagnostics.GroupsEvaluated.Should().Be(0);
    }

    [Fact]
    public void EvaluateRetention_WithMultipleNullCollections_HandlesGracefully()
    {
        // Act - Multiple nulls at once
        var result = _service.EvaluateRetention(null, null, null, null, 1);

        // Assert
        result.Should().NotBeNull();
        result.KeptReleases.Should().BeEmpty();
        result.Diagnostics.InvalidDeploymentsExcluded.Should().Be(0);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(5)]
    [InlineData(100)]
    public void EvaluateRetention_WithVariousPositiveN_Succeeds(int n)
    {
        // Arrange
        var projects = new[] { new Project("P1", "Project 1") };
        var environments = new[] { new Environment("E1", "Env 1") };
        var releases = new[]
        {
            new Release("R1", "P1", "1.0", DateTimeOffset.Parse("2024-01-01T10:00:00Z")),
            new Release("R2", "P1", "2.0", DateTimeOffset.Parse("2024-01-01T11:00:00Z"))
        };
        var deployments = new[]
        {
            new Deployment("D1", "R1", "E1", DateTimeOffset.Parse("2024-01-01T10:30:00Z")),
            new Deployment("D2", "R2", "E1", DateTimeOffset.Parse("2024-01-01T11:30:00Z"))
        };

        // Act
        var result = _service.EvaluateRetention(projects, environments, releases, deployments, n);

        // Assert
        result.Should().NotBeNull();
        result.KeptReleases.Should().HaveCountLessThanOrEqualTo(n);
    }

    [Fact]
    public void EvaluateRetention_WithDuplicateProjectIds_ThrowsValidationException()
    {
        // Arrange
        var projects = new[]
        {
            new Project("P1", "Project 1"),
            new Project("P1", "Duplicate Project")
        };
        var environments = new[] { new Environment("E1", "Env 1") };
        var releases = new[] { new Release("R1", "P1", "1.0", DateTimeOffset.Parse("2024-01-01T10:00:00Z")) };
        var deployments = Array.Empty<Deployment>();

        // Act & Assert
        var ex = Assert.Throws<ValidationException>(() =>
            _service.EvaluateRetention(projects, environments, releases, deployments, 1));
        
        ex.Code.Should().Be("validation.duplicate_id.project");
    }

    [Fact]
    public void EvaluateRetention_WithDuplicateEnvironmentIds_ThrowsValidationException()
    {
        // Arrange
        var projects = new[] { new Project("P1", "Project 1") };
        var environments = new[]
        {
            new Environment("E1", "Env 1"),
            new Environment("E1", "Duplicate Env")
        };
        var releases = new[] { new Release("R1", "P1", "1.0", DateTimeOffset.Parse("2024-01-01T10:00:00Z")) };
        var deployments = Array.Empty<Deployment>();

        // Act & Assert
        var ex = Assert.Throws<ValidationException>(() =>
            _service.EvaluateRetention(projects, environments, releases, deployments, 1));
        
        ex.Code.Should().Be("validation.duplicate_id.environment");
    }

    [Fact]
    public void EvaluateRetention_WithDuplicateReleaseIds_ThrowsValidationException()
    {
        // Arrange
        var projects = new[] { new Project("P1", "Project 1") };
        var environments = new[] { new Environment("E1", "Env 1") };
        var releases = new[]
        {
            new Release("R1", "P1", "1.0", DateTimeOffset.Parse("2024-01-01T10:00:00Z")),
            new Release("R1", "P1", "2.0", DateTimeOffset.Parse("2024-01-01T11:00:00Z"))
        };
        var deployments = Array.Empty<Deployment>();

        // Act & Assert
        var ex = Assert.Throws<ValidationException>(() =>
            _service.EvaluateRetention(projects, environments, releases, deployments, 1));
        
        ex.Code.Should().Be("validation.duplicate_id.release");
    }

    [Fact]
    public void EvaluateRetention_WithNullInProjectsArray_ThrowsValidationException()
    {
        // Arrange
        var projects = new Project[] { null! };
        var environments = new[] { new Environment("E1", "Env 1") };
        var releases = new[] { new Release("R1", "P1", "1.0", DateTimeOffset.Parse("2024-01-01T10:00:00Z")) };
        var deployments = Array.Empty<Deployment>();

        // Act & Assert
        var ex = Assert.Throws<ValidationException>(() =>
            _service.EvaluateRetention(projects, environments, releases, deployments, 1));
        
        ex.Code.Should().Be("validation.null_element");
    }

    [Fact]
    public void EvaluateRetention_WithNullInEnvironmentsArray_ThrowsValidationException()
    {
        // Arrange
        var projects = new[] { new Project("P1", "Project 1") };
        var environments = new Environment[] { null! };
        var releases = new[] { new Release("R1", "P1", "1.0", DateTimeOffset.Parse("2024-01-01T10:00:00Z")) };
        var deployments = Array.Empty<Deployment>();

        // Act & Assert
        var ex = Assert.Throws<ValidationException>(() =>
            _service.EvaluateRetention(projects, environments, releases, deployments, 1));
        
        ex.Code.Should().Be("validation.null_element");
    }

    [Fact]
    public void EvaluateRetention_WithNullInReleasesArray_ThrowsValidationException()
    {
        // Arrange
        var projects = new[] { new Project("P1", "Project 1") };
        var environments = new[] { new Environment("E1", "Env 1") };
        var releases = new Release[] { null! };
        var deployments = Array.Empty<Deployment>();

        // Act & Assert
        var ex = Assert.Throws<ValidationException>(() =>
            _service.EvaluateRetention(projects, environments, releases, deployments, 1));
        
        ex.Code.Should().Be("validation.null_element");
    }

    [Fact]
    public void EvaluateRetention_WithNullInDeploymentsArray_ThrowsValidationException()
    {
        // Arrange
        var projects = new[] { new Project("P1", "Project 1") };
        var environments = new[] { new Environment("E1", "Env 1") };
        var releases = new[] { new Release("R1", "P1", "1.0", DateTimeOffset.Parse("2024-01-01T10:00:00Z")) };
        var deployments = new Deployment[] { null! };

        // Act & Assert
        var ex = Assert.Throws<ValidationException>(() =>
            _service.EvaluateRetention(projects, environments, releases, deployments, 1));
        
        ex.Code.Should().Be("validation.null_element");
    }

    [Fact]
    public void EvaluateRetention_DecisionLogIncludesCorrelationId()
    {
        // Arrange
        var correlationId = "test-corr-123";
        var projects = new[] { new Project("P1", "Project 1") };
        var environments = new[] { new Environment("E1", "Env 1") };
        var releases = new[] { new Release("R1", "P1", "1.0", DateTimeOffset.Parse("2024-01-01T10:00:00Z")) };
        var deployments = new[] { new Deployment("D1", "R1", "E1", DateTimeOffset.Parse("2024-01-01T11:00:00Z")) };

        // Act
        var result = _service.EvaluateRetention(projects, environments, releases, deployments, 1, correlationId);

        // Assert
        result.Decisions.Where(d => d.ReasonCode == "kept.top_n").Should().AllSatisfy(d =>
            d.CorrelationId.Should().Be(correlationId)
        );
    }

    [Fact]
    public void EvaluateRetention_WithManyDeploymentsOfSameRelease_PicksLatest()
    {
        // Arrange
        var projects = new[] { new Project("P1", "Project 1") };
        var environments = new[] { new Environment("E1", "Env 1") };
        var releases = new[] { new Release("R1", "P1", "1.0", DateTimeOffset.Parse("2024-01-01T10:00:00Z")) };
        var deployments = new[]
        {
            new Deployment("D1", "R1", "E1", DateTimeOffset.Parse("2024-01-01T10:00:00Z")),
            new Deployment("D2", "R1", "E1", DateTimeOffset.Parse("2024-01-01T10:30:00Z")),
            new Deployment("D3", "R1", "E1", DateTimeOffset.Parse("2024-01-01T11:00:00Z")),
            new Deployment("D4", "R1", "E1", DateTimeOffset.Parse("2024-01-01T10:15:00Z"))
        };

        // Act
        var result = _service.EvaluateRetention(projects, environments, releases, deployments, 1);

        // Assert
        result.KeptReleases.Should().HaveCount(1);
        result.KeptReleases[0].LatestDeployedAt.Should().Be(DateTimeOffset.Parse("2024-01-01T11:00:00Z"));
    }

    [Fact]
    public void EvaluateRetention_ReleasesWithoutDeployments_NotIncludedInResults()
    {
        // Arrange
        var projects = new[] { new Project("P1", "Project 1") };
        var environments = new[] { new Environment("E1", "Env 1") };
        var releases = new[]
        {
            new Release("R1", "P1", "1.0", DateTimeOffset.Parse("2024-01-01T10:00:00Z")),
            new Release("R2", "P1", "2.0", DateTimeOffset.Parse("2024-01-01T11:00:00Z")),
            new Release("R3", "P1", "3.0", DateTimeOffset.Parse("2024-01-01T12:00:00Z")) // No deployment
        };
        var deployments = new[]
        {
            new Deployment("D1", "R1", "E1", DateTimeOffset.Parse("2024-01-01T10:30:00Z")),
            new Deployment("D2", "R2", "E1", DateTimeOffset.Parse("2024-01-01T11:30:00Z"))
        };

        // Act
        var result = _service.EvaluateRetention(projects, environments, releases, deployments, 2);

        // Assert
        result.KeptReleases.Should().HaveCount(2);
        result.KeptReleases.Should().NotContain(r => r.ReleaseId == "R3");
    }

    [Fact]
    public void EvaluateRetention_SameDateDeployedUsesTieBreakers()
    {
        // Arrange - Two releases deployed at same time, different creation times
        var projects = new[] { new Project("P1", "Project 1") };
        var environments = new[] { new Environment("E1", "Env 1") };
        var releases = new[]
        {
            new Release("R1", "P1", "1.0", DateTimeOffset.Parse("2024-01-01T09:00:00Z")), // Created earlier
            new Release("R2", "P1", "2.0", DateTimeOffset.Parse("2024-01-01T10:00:00Z"))  // Created later
        };
        var deployments = new[]
        {
            new Deployment("D1", "R1", "E1", DateTimeOffset.Parse("2024-01-01T12:00:00Z")), // Same time
            new Deployment("D2", "R2", "E1", DateTimeOffset.Parse("2024-01-01T12:00:00Z"))  // Same time
        };

        // Act
        var result = _service.EvaluateRetention(projects, environments, releases, deployments, 1);

        // Assert - R2 should be kept (created later = higher priority in tie-breaker)
        result.KeptReleases.Should().HaveCount(1);
        result.KeptReleases[0].ReleaseId.Should().Be("R2");
    }

    [Fact]
    public void EvaluateRetention_WithLargeDataset_EvaluatesSuccessfully()
    {
        // Arrange - Large dataset
        var projects = Enumerable.Range(1, 10).Select(i => new Project($"P{i}", $"Project {i}")).ToArray();
        var environments = Enumerable.Range(1, 5).Select(i => new Environment($"E{i}", $"Environment {i}")).ToArray();
        var releases = Enumerable.Range(1, 50).Select(i =>
            new Release($"R{i}", projects[i % projects.Length].Id, $"{i}.0", DateTimeOffset.Parse($"2024-01-{(i % 28) + 1:D2}T10:00:00Z"))
        ).ToArray();
        var deployments = Enumerable.Range(1, 100).Select(i =>
            new Deployment($"D{i}", $"R{(i % releases.Length) + 1}", $"E{(i % environments.Length) + 1}", DateTimeOffset.Parse($"2024-02-{(i % 28) + 1:D2}T10:00:00Z"))
        ).ToArray();

        // Act
        var result = _service.EvaluateRetention(projects, environments, releases, deployments, 3);

        // Assert
        result.Should().NotBeNull();
        result.KeptReleases.Should().NotBeEmpty();
        result.Diagnostics.GroupsEvaluated.Should().BeGreaterThan(0);
    }
}
