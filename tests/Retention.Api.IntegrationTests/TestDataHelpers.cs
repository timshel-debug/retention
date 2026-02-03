using Retention.Api.Contracts;
using System.Text.Json;

namespace Retention.Api.IntegrationTests;

/// <summary>
/// Helper methods for creating test data for API tests.
/// </summary>
public static class TestDataHelpers
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true
    };

    /// <summary>
    /// Creates a valid retention evaluation request with sample data.
    /// </summary>
    public static EvaluateRetentionRequest CreateValidRetentionRequest(
        int n = 1,
        string? correlationId = null)
    {
        return new EvaluateRetentionRequest
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
                    new EnvironmentDto { Id = "Environment-1", Name = "Production" },
                    new EnvironmentDto { Id = "Environment-2", Name = "Staging" }
                },
                Releases = new[]
                {
                    new ReleaseDto { Id = "Release-1", ProjectId = "Project-1", Version = "1.0.0", Created = DateTimeOffset.Parse("2024-01-01T10:00:00Z") },
                    new ReleaseDto { Id = "Release-2", ProjectId = "Project-1", Version = "2.0.0", Created = DateTimeOffset.Parse("2024-01-02T10:00:00Z") },
                    new ReleaseDto { Id = "Release-3", ProjectId = "Project-2", Version = "1.5.0", Created = DateTimeOffset.Parse("2024-01-01T14:00:00Z") }
                },
                Deployments = new[]
                {
                    new DeploymentDto { Id = "Deployment-1", ReleaseId = "Release-1", EnvironmentId = "Environment-1", DeployedAt = DateTimeOffset.Parse("2024-01-01T11:00:00Z") },
                    new DeploymentDto { Id = "Deployment-2", ReleaseId = "Release-2", EnvironmentId = "Environment-1", DeployedAt = DateTimeOffset.Parse("2024-01-02T11:00:00Z") },
                    new DeploymentDto { Id = "Deployment-3", ReleaseId = "Release-3", EnvironmentId = "Environment-2", DeployedAt = DateTimeOffset.Parse("2024-01-01T15:00:00Z") }
                }
            },
            ReleasesToKeep = n,
            CorrelationId = correlationId
        };
    }

    /// <summary>
    /// Creates a request with empty collections.
    /// </summary>
    public static EvaluateRetentionRequest CreateEmptyRequest(int n = 1)
    {
        return new EvaluateRetentionRequest
        {
            Dataset = new DatasetDto
            {
                Projects = Array.Empty<ProjectDto>(),
                Environments = Array.Empty<EnvironmentDto>(),
                Releases = Array.Empty<ReleaseDto>(),
                Deployments = Array.Empty<DeploymentDto>()
            },
            ReleasesToKeep = n
        };
    }

    /// <summary>
    /// Creates a request with invalid n value (negative).
    /// </summary>
    public static EvaluateRetentionRequest CreateRequestWithNegativeN(int negativeN = -1)
    {
        return new EvaluateRetentionRequest
        {
            Dataset = new DatasetDto
            {
                Projects = Array.Empty<ProjectDto>(),
                Environments = Array.Empty<EnvironmentDto>(),
                Releases = Array.Empty<ReleaseDto>(),
                Deployments = Array.Empty<DeploymentDto>()
            },
            ReleasesToKeep = negativeN
        };
    }

    /// <summary>
    /// Creates a request with invalid references (missing project).
    /// </summary>
    public static EvaluateRetentionRequest CreateRequestWithInvalidReferences()
    {
        return new EvaluateRetentionRequest
        {
            Dataset = new DatasetDto
            {
                Projects = new[] { new ProjectDto { Id = "Project-1", Name = "Project 1" } },
                Environments = new[] { new EnvironmentDto { Id = "Environment-1", Name = "Production" } },
                Releases = new[]
                {
                    new ReleaseDto { Id = "Release-1", ProjectId = "NonExistentProject", Version = "1.0.0", Created = DateTimeOffset.Parse("2024-01-01T10:00:00Z") }
                },
                Deployments = new[]
                {
                    new DeploymentDto { Id = "Deployment-1", ReleaseId = "Release-1", EnvironmentId = "Environment-1", DeployedAt = DateTimeOffset.Parse("2024-01-01T11:00:00Z") }
                }
            },
            ReleasesToKeep = 1
        };
    }
}
