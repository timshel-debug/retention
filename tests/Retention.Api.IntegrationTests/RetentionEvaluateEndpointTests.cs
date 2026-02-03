using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Retention.Api.Contracts;

namespace Retention.Api.IntegrationTests;

/// <summary>
/// Integration tests for POST /api/v1/retention/evaluate endpoint.
/// </summary>
public class RetentionEvaluateEndpointTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client;
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true
    };

    public RetentionEvaluateEndpointTests(WebApplicationFactory<Program> factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Post_Evaluate_WithValidRequest_Returns200()
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
        result!.KeptReleases.Should().HaveCount(1);
        result.KeptReleases[0].ReleaseId.Should().Be("Release-1");
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(-100)]
    [InlineData(int.MinValue)]
    public async Task Post_Evaluate_WithNegativeReleasesToKeep_Returns400WithProblemDetails(int invalidN)
    {
        // Arrange
        var request = new EvaluateRetentionRequest
        {
            Dataset = new DatasetDto
            {
                Projects = Array.Empty<ProjectDto>(),
                Environments = Array.Empty<EnvironmentDto>(),
                Releases = Array.Empty<ReleaseDto>(),
                Deployments = Array.Empty<DeploymentDto>()
            },
            ReleasesToKeep = invalidN
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/v1/retention/evaluate", request, JsonOptions);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain("validation.n_negative");
    }

    [Fact]
    public async Task Post_Evaluate_WithZeroReleasesToKeep_Returns200WithEmptyKept()
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
            ReleasesToKeep = 0
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/v1/retention/evaluate", request, JsonOptions);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<EvaluateRetentionResponse>(JsonOptions);
        result.Should().NotBeNull();
        result!.KeptReleases.Should().BeEmpty();
    }

    [Fact]
    public async Task Post_Evaluate_WithMalformedJson_Returns400()
    {
        // Arrange
        var invalidJson = new StringContent("{ invalid json }", System.Text.Encoding.UTF8, "application/json");

        // Act
        var response = await _client.PostAsync("/api/v1/retention/evaluate", invalidJson);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Post_Evaluate_WithCorrelationId_ReturnsCorrelationIdInResponse()
    {
        // Arrange
        var correlationId = "test-correlation-123";
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
            ReleasesToKeep = 1,
            CorrelationId = correlationId
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/v1/retention/evaluate", request, JsonOptions);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<EvaluateRetentionResponse>(JsonOptions);
        result.Should().NotBeNull();
        result!.CorrelationId.Should().Be(correlationId);
    }
}
