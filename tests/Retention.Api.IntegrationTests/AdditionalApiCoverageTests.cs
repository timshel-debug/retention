using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Retention.Api.Contracts;

namespace Retention.Api.IntegrationTests;

/// <summary>
/// Integration tests targeting remaining API coverage gaps.
/// </summary>
public class AdditionalApiCoverageTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client;
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true
    };

    public AdditionalApiCoverageTests(WebApplicationFactory<Program> factory)
    {
        _client = factory.CreateClient();
    }

    [Theory]
    [InlineData(10)]
    [InlineData(50)]
    [InlineData(999)]
    public async Task Evaluate_WithVariousPositiveReleasesToKeep_Returns200(int n)
    {
        // Arrange
        var request = new EvaluateRetentionRequest
        {
            Dataset = new DatasetDto
            {
                Projects = new[] { new ProjectDto { Id = "P1", Name = "Proj 1" } },
                Environments = new[] { new EnvironmentDto { Id = "E1", Name = "Env 1" } },
                Releases = new[]
                {
                    new ReleaseDto { Id = "R1", ProjectId = "P1", Version = "1.0", Created = DateTimeOffset.Parse("2024-01-01T10:00:00Z") }
                },
                Deployments = new[]
                {
                    new DeploymentDto { Id = "D1", ReleaseId = "R1", EnvironmentId = "E1", DeployedAt = DateTimeOffset.Parse("2024-01-01T10:30:00Z") }
                }
            },
            ReleasesToKeep = n
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/v1/retention/evaluate", request, JsonOptions);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<EvaluateRetentionResponse>(JsonOptions);
        result.Should().NotBeNull();
    }

    [Fact]
    public async Task Validate_WithValidComplexDataset_ReturnsValidTrue()
    {
        // Arrange - Large valid dataset
        var projects = Enumerable.Range(1, 5).Select(i => new ProjectDto { Id = $"P{i}", Name = $"Project {i}" }).ToArray();
        var environments = Enumerable.Range(1, 3).Select(i => new EnvironmentDto { Id = $"E{i}", Name = $"Environment {i}" }).ToArray();
        var releases = Enumerable.Range(1, 10).Select(i => new ReleaseDto 
        { 
            Id = $"R{i}", 
            ProjectId = projects[i % projects.Length].Id, 
            Version = $"{i}.0",
            Created = DateTimeOffset.Parse($"2024-01-{i:D2}T10:00:00Z")
        }).ToArray();

        var request = new ValidateDatasetRequest
        {
            Dataset = new DatasetDto
            {
                Projects = projects,
                Environments = environments,
                Releases = releases,
                Deployments = Array.Empty<DeploymentDto>()
            }
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/v1/datasets/validate", request, JsonOptions);
        var result = await response.Content.ReadFromJsonAsync<ValidateDatasetResponse>(JsonOptions);

        // Assert
        result.Should().NotBeNull();
        result!.IsValid.Should().BeTrue();
        result.Errors.Should().BeEmpty();
    }

    [Fact]
    public async Task Evaluate_WithMixedValidAndInvalidDeployments_ReturnsPartialResults()
    {
        // Arrange
        var request = new EvaluateRetentionRequest
        {
            Dataset = new DatasetDto
            {
                Projects = new[] { new ProjectDto { Id = "P1", Name = "Proj 1" } },
                Environments = new[] { new EnvironmentDto { Id = "E1", Name = "Env 1" } },
                Releases = new[]
                {
                    new ReleaseDto { Id = "R1", ProjectId = "P1", Version = "1.0", Created = DateTimeOffset.Parse("2024-01-01T10:00:00Z") },
                    new ReleaseDto { Id = "R2", ProjectId = "P1", Version = "2.0", Created = DateTimeOffset.Parse("2024-01-01T11:00:00Z") }
                },
                Deployments = new[]
                {
                    // Valid deployment
                    new DeploymentDto { Id = "D1", ReleaseId = "R1", EnvironmentId = "E1", DeployedAt = DateTimeOffset.Parse("2024-01-01T10:30:00Z") },
                    // Invalid - missing environment
                    new DeploymentDto { Id = "D2", ReleaseId = "R2", EnvironmentId = "E-INVALID", DeployedAt = DateTimeOffset.Parse("2024-01-01T11:30:00Z") },
                    // Invalid - missing release
                    new DeploymentDto { Id = "D3", ReleaseId = "R-INVALID", EnvironmentId = "E1", DeployedAt = DateTimeOffset.Parse("2024-01-01T12:00:00Z") }
                }
            },
            ReleasesToKeep = 2
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/v1/retention/evaluate", request, JsonOptions);
        var result = await response.Content.ReadFromJsonAsync<EvaluateRetentionResponse>(JsonOptions);

        // Assert - Should process valid deployment, exclude invalid ones
        result.Should().NotBeNull();
        result!.KeptReleases.Should().HaveCount(1);
        result.Diagnostics.InvalidDeploymentsExcluded.Should().Be(2);
    }

    [Fact]
    public async Task Evaluate_WithMultipleProjectsMultipleEnvironments_GroupsCorrectly()
    {
        // Arrange
        var request = new EvaluateRetentionRequest
        {
            Dataset = new DatasetDto
            {
                Projects = new[]
                {
                    new ProjectDto { Id = "P1", Name = "Proj 1" },
                    new ProjectDto { Id = "P2", Name = "Proj 2" }
                },
                Environments = new[]
                {
                    new EnvironmentDto { Id = "E1", Name = "Env 1" },
                    new EnvironmentDto { Id = "E2", Name = "Env 2" }
                },
                Releases = new[]
                {
                    new ReleaseDto { Id = "R1", ProjectId = "P1", Version = "1.0", Created = DateTimeOffset.Parse("2024-01-01T10:00:00Z") },
                    new ReleaseDto { Id = "R2", ProjectId = "P1", Version = "1.1", Created = DateTimeOffset.Parse("2024-01-01T11:00:00Z") },
                    new ReleaseDto { Id = "R3", ProjectId = "P2", Version = "2.0", Created = DateTimeOffset.Parse("2024-01-01T12:00:00Z") },
                    new ReleaseDto { Id = "R4", ProjectId = "P2", Version = "2.1", Created = DateTimeOffset.Parse("2024-01-01T13:00:00Z") }
                },
                Deployments = new[]
                {
                    // P1/E1
                    new DeploymentDto { Id = "D1", ReleaseId = "R1", EnvironmentId = "E1", DeployedAt = DateTimeOffset.Parse("2024-01-01T10:30:00Z") },
                    new DeploymentDto { Id = "D2", ReleaseId = "R2", EnvironmentId = "E1", DeployedAt = DateTimeOffset.Parse("2024-01-01T11:30:00Z") },
                    // P1/E2
                    new DeploymentDto { Id = "D3", ReleaseId = "R1", EnvironmentId = "E2", DeployedAt = DateTimeOffset.Parse("2024-01-01T10:45:00Z") },
                    new DeploymentDto { Id = "D4", ReleaseId = "R2", EnvironmentId = "E2", DeployedAt = DateTimeOffset.Parse("2024-01-01T11:45:00Z") },
                    // P2/E1
                    new DeploymentDto { Id = "D5", ReleaseId = "R3", EnvironmentId = "E1", DeployedAt = DateTimeOffset.Parse("2024-01-01T12:30:00Z") },
                    new DeploymentDto { Id = "D6", ReleaseId = "R4", EnvironmentId = "E1", DeployedAt = DateTimeOffset.Parse("2024-01-01T13:30:00Z") },
                    // P2/E2
                    new DeploymentDto { Id = "D7", ReleaseId = "R3", EnvironmentId = "E2", DeployedAt = DateTimeOffset.Parse("2024-01-01T12:45:00Z") },
                    new DeploymentDto { Id = "D8", ReleaseId = "R4", EnvironmentId = "E2", DeployedAt = DateTimeOffset.Parse("2024-01-01T13:45:00Z") }
                }
            },
            ReleasesToKeep = 1
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/v1/retention/evaluate", request, JsonOptions);
        var result = await response.Content.ReadFromJsonAsync<EvaluateRetentionResponse>(JsonOptions);

        // Assert - Should have 4 groups (P1/E1, P1/E2, P2/E1, P2/E2), 1 per group = 4 total
        result.Should().NotBeNull();
        result!.KeptReleases.Should().HaveCount(4);
        result.Diagnostics.GroupsEvaluated.Should().Be(4);
        // Each group has exactly 1 kept release with rank 1
        result.KeptReleases.Should().AllSatisfy(r => r.Rank.Should().Be(1));
    }

    [Fact]
    public async Task Evaluate_DecisionLogContainsDecisionsForKeptReleases()
    {
        // Arrange
        var request = new EvaluateRetentionRequest
        {
            Dataset = new DatasetDto
            {
                Projects = new[] { new ProjectDto { Id = "P1", Name = "Proj 1" } },
                Environments = new[] { new EnvironmentDto { Id = "E1", Name = "Env 1" } },
                Releases = new[]
                {
                    new ReleaseDto { Id = "R1", ProjectId = "P1", Version = "1.0", Created = DateTimeOffset.Parse("2024-01-01T10:00:00Z") }
                },
                Deployments = new[]
                {
                    new DeploymentDto { Id = "D1", ReleaseId = "R1", EnvironmentId = "E1", DeployedAt = DateTimeOffset.Parse("2024-01-01T10:30:00Z") }
                }
            },
            ReleasesToKeep = 1
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/v1/retention/evaluate", request, JsonOptions);
        var result = await response.Content.ReadFromJsonAsync<EvaluateRetentionResponse>(JsonOptions);

        // Assert
        result.Should().NotBeNull();
        result!.Decisions.Should().NotBeEmpty();
        var kept = result.Decisions.Where(d => d.ReasonCode == "kept.top_n").ToList();
        kept.Should().HaveCount(1);
        kept[0].ReleaseId.Should().Be("R1");
    }

    [Fact]
    public async Task Validate_ErrorsContainValidationMessageDetails()
    {
        // Arrange
        var request = new ValidateDatasetRequest
        {
            Dataset = new DatasetDto
            {
                Projects = new[]
                {
                    new ProjectDto { Id = "P1", Name = "Proj 1" },
                    new ProjectDto { Id = "P1", Name = "Duplicate" }
                },
                Environments = Array.Empty<EnvironmentDto>(),
                Releases = Array.Empty<ReleaseDto>(),
                Deployments = Array.Empty<DeploymentDto>()
            }
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/v1/datasets/validate", request, JsonOptions);
        var result = await response.Content.ReadFromJsonAsync<ValidateDatasetResponse>(JsonOptions);

        // Assert
        result.Should().NotBeNull();
        result!.Errors.Should().NotBeEmpty();
        result.Errors[0].Code.Should().NotBeNullOrEmpty();
        result.Errors[0].Message.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task Evaluate_WithCorrelationIdIncludedInDecisions()
    {
        // Arrange
        var request = new EvaluateRetentionRequest
        {
            Dataset = new DatasetDto
            {
                Projects = new[] { new ProjectDto { Id = "P1", Name = "Proj 1" } },
                Environments = new[] { new EnvironmentDto { Id = "E1", Name = "Env 1" } },
                Releases = new[]
                {
                    new ReleaseDto { Id = "R1", ProjectId = "P1", Version = "1.0", Created = DateTimeOffset.Parse("2024-01-01T10:00:00Z") }
                },
                Deployments = new[]
                {
                    new DeploymentDto { Id = "D1", ReleaseId = "R1", EnvironmentId = "E1", DeployedAt = DateTimeOffset.Parse("2024-01-01T10:30:00Z") }
                }
            },
            ReleasesToKeep = 1,
            CorrelationId = "my-correlation-id-12345"
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/v1/retention/evaluate", request, JsonOptions);
        var result = await response.Content.ReadFromJsonAsync<EvaluateRetentionResponse>(JsonOptions);

        // Assert
        result.Should().NotBeNull();
        result!.CorrelationId.Should().Be("my-correlation-id-12345");
    }
}
