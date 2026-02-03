using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Retention.Api.Contracts;

namespace Retention.Api.IntegrationTests;

/// <summary>
/// Integration tests for POST /api/v1/datasets/validate endpoint.
/// </summary>
public class DatasetValidateEndpointTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client;
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true
    };

    public DatasetValidateEndpointTests(WebApplicationFactory<Program> factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Post_Validate_WithValidDataset_Returns200AndIsValidTrue()
    {
        // Arrange
        var request = new ValidateDatasetRequest
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
            }
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/v1/datasets/validate", request, JsonOptions);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<ValidateDatasetResponse>(JsonOptions);
        result.Should().NotBeNull();
        result!.IsValid.Should().BeTrue();
        result.Errors.Should().BeEmpty();
    }

    [Fact]
    public async Task Post_Validate_WithDuplicateProjectIds_Returns200AndIsValidFalse()
    {
        // Arrange
        var request = new ValidateDatasetRequest
        {
            Dataset = new DatasetDto
            {
                Projects = new[]
                {
                    new ProjectDto { Id = "Project-1", Name = "Project 1" },
                    new ProjectDto { Id = "Project-1", Name = "Duplicate Project" }
                },
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
        result!.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.Code == "validation.duplicate_project_id");
    }

    [Fact]
    public async Task Post_Validate_WithMissingProjectReference_Returns200WithWarning()
    {
        // Arrange
        var request = new ValidateDatasetRequest
        {
            Dataset = new DatasetDto
            {
                Projects = new[] { new ProjectDto { Id = "Project-1", Name = "Project 1" } },
                Environments = new[] { new EnvironmentDto { Id = "Environment-1", Name = "Production" } },
                Releases = new[]
                {
                    new ReleaseDto { Id = "Release-1", ProjectId = "NonExistent-Project", Version = "1.0.0", Created = DateTimeOffset.Parse("2024-01-01T10:00:00Z") }
                },
                Deployments = Array.Empty<DeploymentDto>()
            }
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/v1/datasets/validate", request, JsonOptions);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<ValidateDatasetResponse>(JsonOptions);
        result.Should().NotBeNull();
        // Per ADR-0005, invalid references are warnings, not errors - so dataset is still valid
        result!.IsValid.Should().BeTrue();
        result.Warnings.Should().Contain(w => w.Code == "validation.invalid_reference");
    }

    [Fact]
    public async Task Post_Validate_WithEmptyDataset_Returns200AndIsValidTrue()
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
        result.Errors.Should().BeEmpty();
    }

    [Fact]
    public async Task Post_Validate_ErrorsAreDeterministicallySorted()
    {
        // Arrange - Create dataset with multiple validation errors
        var request = new ValidateDatasetRequest
        {
            Dataset = new DatasetDto
            {
                Projects = new[]
                {
                    new ProjectDto { Id = "Project-B", Name = "B" },
                    new ProjectDto { Id = "Project-A", Name = "A" },
                    new ProjectDto { Id = "Project-A", Name = "A Duplicate" } // Duplicate
                },
                Environments = new[]
                {
                    new EnvironmentDto { Id = "Environment-1", Name = "Prod" },
                    new EnvironmentDto { Id = "Environment-1", Name = "Prod Duplicate" } // Duplicate
                },
                Releases = Array.Empty<ReleaseDto>(),
                Deployments = Array.Empty<DeploymentDto>()
            }
        };

        // Act - Run twice
        var response1 = await _client.PostAsJsonAsync("/api/v1/datasets/validate", request, JsonOptions);
        var response2 = await _client.PostAsJsonAsync("/api/v1/datasets/validate", request, JsonOptions);

        // Assert - Both should have identical error ordering
        var result1 = await response1.Content.ReadFromJsonAsync<ValidateDatasetResponse>(JsonOptions);
        var result2 = await response2.Content.ReadFromJsonAsync<ValidateDatasetResponse>(JsonOptions);

        var json1 = JsonSerializer.Serialize(result1, JsonOptions);
        var json2 = JsonSerializer.Serialize(result2, JsonOptions);

        json1.Should().Be(json2, "error ordering must be deterministic");
    }
}
