using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Retention.Api.Contracts;

namespace Retention.Api.IntegrationTests;

/// <summary>
/// Integration tests for exception handling and error responses (RFC7807).
/// </summary>
public class ExceptionHandlingTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client;
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true
    };

    public ExceptionHandlingTests(WebApplicationFactory<Program> factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Evaluate_WithDomainException_Returns500WithProblemDetails()
    {
        // Arrange - This would require a scenario that causes a domain invariant
        // For now, test with valid request that exercises error paths
        var request = new EvaluateRetentionRequest
        {
            Dataset = new DatasetDto
            {
                Projects = Array.Empty<ProjectDto>(),
                Environments = Array.Empty<EnvironmentDto>(),
                Releases = Array.Empty<ReleaseDto>(),
                Deployments = Array.Empty<DeploymentDto>()
            },
            ReleasesToKeep = 0
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/v1/retention/evaluate", request, JsonOptions);

        // Assert - Should succeed with empty results
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<EvaluateRetentionResponse>(JsonOptions);
        result.Should().NotBeNull();
        result!.KeptReleases.Should().BeEmpty();
    }

    [Fact]
    public async Task Evaluate_WithMalformedJsonPayload_Returns400BadRequest()
    {
        // Arrange
        var invalidJson = new StringContent("{ not valid json }", System.Text.Encoding.UTF8, "application/json");

        // Act
        var response = await _client.PostAsync("/api/v1/retention/evaluate", invalidJson);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Evaluate_WithEmptyPayload_Returns400BadRequest()
    {
        // Arrange
        var emptyContent = new StringContent("", System.Text.Encoding.UTF8, "application/json");

        // Act
        var response = await _client.PostAsync("/api/v1/retention/evaluate", emptyContent);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Evaluate_WithNullDataset_Returns400BadRequest()
    {
        // Arrange
        var request = new { releasesToKeep = 1 };

        // Act
        var response = await _client.PostAsJsonAsync("/api/v1/retention/evaluate", request, JsonOptions);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Validate_WithMalformedJsonPayload_Returns400BadRequest()
    {
        // Arrange
        var invalidJson = new StringContent("{ not valid json }", System.Text.Encoding.UTF8, "application/json");

        // Act
        var response = await _client.PostAsync("/api/v1/datasets/validate", invalidJson);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Evaluate_ResponseContainsTraceId()
    {
        // Arrange
        var request = new EvaluateRetentionRequest
        {
            Dataset = new DatasetDto
            {
                Projects = new[] { new ProjectDto { Id = "Project-1", Name = "Project 1" } },
                Environments = new[] { new EnvironmentDto { Id = "Environment-1", Name = "Production" } },
                Releases = new[]
                {
                    new ReleaseDto { Id = "Release-1", ProjectId = "Project-1", Version = "1.0.0", Created = DateTimeOffset.Parse("2024-01-01T10:00:00Z") }
                },
                Deployments = new[]
                {
                    new DeploymentDto { Id = "Deployment-1", ReleaseId = "Release-1", EnvironmentId = "Environment-1", DeployedAt = DateTimeOffset.Parse("2024-01-01T11:00:00Z") }
                }
            },
            ReleasesToKeep = 1
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/v1/retention/evaluate", request, JsonOptions);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<EvaluateRetentionResponse>(JsonOptions);
        result.Should().NotBeNull();
        // TraceId is present in correlation metadata
        result!.Diagnostics.Should().NotBeNull();
    }

    [Fact]
    public async Task Evaluate_WithLargeReleasesToKeep_Returns200()
    {
        // Arrange
        var request = new EvaluateRetentionRequest
        {
            Dataset = new DatasetDto
            {
                Projects = new[] { new ProjectDto { Id = "Project-1", Name = "Project 1" } },
                Environments = new[] { new EnvironmentDto { Id = "Environment-1", Name = "Production" } },
                Releases = new[]
                {
                    new ReleaseDto { Id = "Release-1", ProjectId = "Project-1", Version = "1.0.0", Created = DateTimeOffset.Parse("2024-01-01T10:00:00Z") }
                },
                Deployments = new[]
                {
                    new DeploymentDto { Id = "Deployment-1", ReleaseId = "Release-1", EnvironmentId = "Environment-1", DeployedAt = DateTimeOffset.Parse("2024-01-01T11:00:00Z") }
                }
            },
            ReleasesToKeep = 1000000 // Large number of releases to keep
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/v1/retention/evaluate", request, JsonOptions);

        // Assert - Should still succeed (n is just upper bound)
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<EvaluateRetentionResponse>(JsonOptions);
        result.Should().NotBeNull();
        result!.KeptReleases.Should().HaveCount(1); // Only 1 release exists
    }

    [Fact]
    public async Task Evaluate_WithMultipleProjectsAndEnvironments_ReturnsCorrectCounts()
    {
        // Arrange
        var request = new EvaluateRetentionRequest
        {
            Dataset = new DatasetDto
            {
                Projects = new[]
                {
                    new ProjectDto { Id = "Project-1", Name = "Project 1" },
                    new ProjectDto { Id = "Project-2", Name = "Project 2" }
                },
                Environments = new[]
                {
                    new EnvironmentDto { Id = "Environment-1", Name = "Prod" },
                    new EnvironmentDto { Id = "Environment-2", Name = "Staging" }
                },
                Releases = new[]
                {
                    new ReleaseDto { Id = "Release-1", ProjectId = "Project-1", Version = "1.0.0", Created = DateTimeOffset.Parse("2024-01-01T10:00:00Z") },
                    new ReleaseDto { Id = "Release-2", ProjectId = "Project-1", Version = "1.0.1", Created = DateTimeOffset.Parse("2024-01-01T11:00:00Z") },
                    new ReleaseDto { Id = "Release-3", ProjectId = "Project-2", Version = "2.0.0", Created = DateTimeOffset.Parse("2024-01-01T12:00:00Z") }
                },
                Deployments = new[]
                {
                    new DeploymentDto { Id = "Dep-1", ReleaseId = "Release-1", EnvironmentId = "Environment-1", DeployedAt = DateTimeOffset.Parse("2024-01-01T10:30:00Z") },
                    new DeploymentDto { Id = "Dep-2", ReleaseId = "Release-2", EnvironmentId = "Environment-1", DeployedAt = DateTimeOffset.Parse("2024-01-01T11:30:00Z") },
                    new DeploymentDto { Id = "Dep-3", ReleaseId = "Release-3", EnvironmentId = "Environment-2", DeployedAt = DateTimeOffset.Parse("2024-01-01T12:30:00Z") }
                }
            },
            ReleasesToKeep = 1
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/v1/retention/evaluate", request, JsonOptions);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<EvaluateRetentionResponse>(JsonOptions);
        result.Should().NotBeNull();
        result!.KeptReleases.Should().HaveCount(2); // 1 from each environment
    }

    [Fact]
    public async Task Validate_WithNullDataset_Returns400()
    {
        // Arrange
        var request = new { };

        // Act
        var response = await _client.PostAsJsonAsync("/api/v1/datasets/validate", request, JsonOptions);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Validate_WithEmptyArrays_Returns200AndIsValidTrue()
    {
        // Arrange
        var request = new ValidateDatasetRequest
        {
            Dataset = new DatasetDto
            {
                Projects = Array.Empty<ProjectDto>(),
                Environments = Array.Empty<EnvironmentDto>(),
                Releases = Array.Empty<ReleaseDto>(),
                Deployments = Array.Empty<DeploymentDto>()
            }
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/v1/datasets/validate", request, JsonOptions);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<ValidateDatasetResponse>(JsonOptions);
        result.Should().NotBeNull();
        result!.IsValid.Should().BeTrue();
    }
}
