using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Retention.Api.Contracts;

namespace Retention.Api.IntegrationTests;

/// <summary>
/// Extended integration tests for API code path coverage.
/// </summary>
public class ApiCoverageExtensionTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client;
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true
    };

    public ApiCoverageExtensionTests(WebApplicationFactory<Program> factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Evaluate_WithMultipleEnvironmentsPerRelease_KeepsOnePerEnvironment()
    {
        // Arrange
        var request = new EvaluateRetentionRequest
        {
            Dataset = new DatasetDto
            {
                Projects = new[] { new ProjectDto { Id = "P1", Name = "Proj 1" } },
                Environments = new[]
                {
                    new EnvironmentDto { Id = "E1", Name = "Env 1" },
                    new EnvironmentDto { Id = "E2", Name = "Env 2" }
                },
                Releases = new[]
                {
                    new ReleaseDto { Id = "R1", ProjectId = "P1", Version = "1.0", Created = DateTimeOffset.Parse("2024-01-01T10:00:00Z") },
                    new ReleaseDto { Id = "R2", ProjectId = "P1", Version = "2.0", Created = DateTimeOffset.Parse("2024-01-01T11:00:00Z") }
                },
                Deployments = new[]
                {
                    new DeploymentDto { Id = "D1", ReleaseId = "R1", EnvironmentId = "E1", DeployedAt = DateTimeOffset.Parse("2024-01-01T10:30:00Z") },
                    new DeploymentDto { Id = "D2", ReleaseId = "R2", EnvironmentId = "E1", DeployedAt = DateTimeOffset.Parse("2024-01-01T11:30:00Z") },
                    new DeploymentDto { Id = "D3", ReleaseId = "R1", EnvironmentId = "E2", DeployedAt = DateTimeOffset.Parse("2024-01-01T12:00:00Z") },
                    new DeploymentDto { Id = "D4", ReleaseId = "R2", EnvironmentId = "E2", DeployedAt = DateTimeOffset.Parse("2024-01-01T12:30:00Z") }
                }
            },
            ReleasesToKeep = 1
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/v1/retention/evaluate", request, JsonOptions);
        var result = await response.Content.ReadFromJsonAsync<EvaluateRetentionResponse>(JsonOptions);

        // Assert
        result.Should().NotBeNull();
        result!.KeptReleases.Should().HaveCount(2); // R2 from E1, R2 from E2
        var e1Releases = result.KeptReleases.Where(r => r.EnvironmentId == "E1").ToList();
        var e2Releases = result.KeptReleases.Where(r => r.EnvironmentId == "E2").ToList();
        e1Releases.Should().HaveCount(1);
        e2Releases.Should().HaveCount(1);
    }

    [Fact]
    public async Task Evaluate_WithDuplicateDeploymentsOfSameRelease_UsesLatestDeployment()
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
                    new DeploymentDto { Id = "D1", ReleaseId = "R1", EnvironmentId = "E1", DeployedAt = DateTimeOffset.Parse("2024-01-01T10:00:00Z") },
                    new DeploymentDto { Id = "D2", ReleaseId = "R1", EnvironmentId = "E1", DeployedAt = DateTimeOffset.Parse("2024-01-01T10:30:00Z") },
                    new DeploymentDto { Id = "D3", ReleaseId = "R1", EnvironmentId = "E1", DeployedAt = DateTimeOffset.Parse("2024-01-01T11:00:00Z") }
                }
            },
            ReleasesToKeep = 1
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/v1/retention/evaluate", request, JsonOptions);
        var result = await response.Content.ReadFromJsonAsync<EvaluateRetentionResponse>(JsonOptions);

        // Assert - Should use latest deployment (11:00:00Z)
        result.Should().NotBeNull();
        result!.KeptReleases.Should().HaveCount(1);
        result.KeptReleases[0].LatestDeployedAt.Should().Be(DateTimeOffset.Parse("2024-01-01T11:00:00Z"));
    }

    [Fact]
    public async Task Evaluate_WithMissingProjectInRelease_ExcludesDeployment()
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
                    new ReleaseDto { Id = "R1", ProjectId = "P-Missing", Version = "1.0", Created = DateTimeOffset.Parse("2024-01-01T10:00:00Z") }
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
        result!.KeptReleases.Should().BeEmpty();
        result.Diagnostics.InvalidDeploymentsExcluded.Should().Be(1);
    }

    [Fact]
    public async Task Validate_WithValidDatasetAndDeployments_ReturnsValid()
    {
        // Arrange
        var request = new ValidateDatasetRequest
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
            }
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/v1/datasets/validate", request, JsonOptions);
        var result = await response.Content.ReadFromJsonAsync<ValidateDatasetResponse>(JsonOptions);

        // Assert
        result.Should().NotBeNull();
        result!.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task Evaluate_WithReleaseCreatedAfterDeployment_StillKeepsItByTimestamp()
    {
        // Arrange - Test deployment timestamp precedence
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
        result!.KeptReleases.Should().HaveCount(1);
        result.KeptReleases[0].LatestDeployedAt.Should().Be(DateTimeOffset.Parse("2024-01-01T10:30:00Z"));
    }

    [Fact]
    public async Task Evaluate_WithReleasesButNoDeployments_ReturnsEmpty()
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
                Deployments = Array.Empty<DeploymentDto>() // No deployments
            },
            ReleasesToKeep = 1
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/v1/retention/evaluate", request, JsonOptions);
        var result = await response.Content.ReadFromJsonAsync<EvaluateRetentionResponse>(JsonOptions);

        // Assert - Releases with no deployments aren't eligible
        result.Should().NotBeNull();
        result!.KeptReleases.Should().BeEmpty();
        result.Diagnostics.GroupsEvaluated.Should().Be(0);
    }

    [Fact]
    public async Task Validate_WithDuplicateEnvironmentIds_Returns400()
    {
        // Arrange
        var request = new ValidateDatasetRequest
        {
            Dataset = new DatasetDto
            {
                Projects = Array.Empty<ProjectDto>(),
                Environments = new[]
                {
                    new EnvironmentDto { Id = "E1", Name = "Env 1" },
                    new EnvironmentDto { Id = "E1", Name = "Duplicate Env" }
                },
                Releases = Array.Empty<ReleaseDto>(),
                Deployments = Array.Empty<DeploymentDto>()
            }
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/v1/datasets/validate", request, JsonOptions);
        var result = await response.Content.ReadFromJsonAsync<ValidateDatasetResponse>(JsonOptions);

        // Assert
        result.Should().NotBeNull();
        result!.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.Code == "validation.duplicate_environment_id");
    }

    [Fact]
    public async Task Validate_WithDuplicateReleaseIds_Returns400()
    {
        // Arrange
        var request = new ValidateDatasetRequest
        {
            Dataset = new DatasetDto
            {
                Projects = Array.Empty<ProjectDto>(),
                Environments = Array.Empty<EnvironmentDto>(),
                Releases = new[]
                {
                    new ReleaseDto { Id = "R1", ProjectId = "P1", Version = "1.0", Created = DateTimeOffset.Parse("2024-01-01T10:00:00Z") },
                    new ReleaseDto { Id = "R1", ProjectId = "P1", Version = "2.0", Created = DateTimeOffset.Parse("2024-01-01T11:00:00Z") }
                },
                Deployments = Array.Empty<DeploymentDto>()
            }
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/v1/datasets/validate", request, JsonOptions);
        var result = await response.Content.ReadFromJsonAsync<ValidateDatasetResponse>(JsonOptions);

        // Assert
        result.Should().NotBeNull();
        result!.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.Code == "validation.duplicate_release_id");
    }

    [Fact]
    public async Task Evaluate_WithSameTimestampAcrossReleases_UsesCreatedThenIdTieBreaker()
    {
        // Arrange - Two releases deployed at same time
        var request = new EvaluateRetentionRequest
        {
            Dataset = new DatasetDto
            {
                Projects = new[] { new ProjectDto { Id = "P1", Name = "Proj 1" } },
                Environments = new[] { new EnvironmentDto { Id = "E1", Name = "Env 1" } },
                Releases = new[]
                {
                    new ReleaseDto { Id = "B-Release", ProjectId = "P1", Version = "1.0", Created = DateTimeOffset.Parse("2024-01-01T10:00:00Z") },
                    new ReleaseDto { Id = "A-Release", ProjectId = "P1", Version = "2.0", Created = DateTimeOffset.Parse("2024-01-01T09:00:00Z") }
                },
                Deployments = new[]
                {
                    new DeploymentDto { Id = "D1", ReleaseId = "B-Release", EnvironmentId = "E1", DeployedAt = DateTimeOffset.Parse("2024-01-01T12:00:00Z") },
                    new DeploymentDto { Id = "D2", ReleaseId = "A-Release", EnvironmentId = "E1", DeployedAt = DateTimeOffset.Parse("2024-01-01T12:00:00Z") }
                }
            },
            ReleasesToKeep = 1
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/v1/retention/evaluate", request, JsonOptions);
        var result = await response.Content.ReadFromJsonAsync<EvaluateRetentionResponse>(JsonOptions);

        // Assert - B-Release has later Created timestamp, so it wins
        result.Should().NotBeNull();
        result!.KeptReleases.Should().HaveCount(1);
        result.KeptReleases[0].ReleaseId.Should().Be("B-Release");
    }

    [Fact]
    public async Task Evaluate_WithMoreReleasesRequiredThanEligible_ReturnsAllEligible()
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
            ReleasesToKeep = 100 // Request more than available
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/v1/retention/evaluate", request, JsonOptions);
        var result = await response.Content.ReadFromJsonAsync<EvaluateRetentionResponse>(JsonOptions);

        // Assert - Only returns 1 (all eligible)
        result.Should().NotBeNull();
        result!.KeptReleases.Should().HaveCount(1);
    }
}
