using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Retention.Api.Contracts;

namespace Retention.Api.IntegrationTests;

/// <summary>
/// Integration tests proving deterministic output ordering.
/// Inputs can arrive in any order; outputs must be stable-sorted.
/// </summary>
public class DeterminismTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client;
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true
    };

    public DeterminismTests(WebApplicationFactory<Program> factory)
    {
        _client = factory.CreateClient();
    }

    /// <summary>
    /// Verifies that shuffled input produces identical output.
    /// REQ: Identical input sets (regardless of order) produce byte-identical output.
    /// </summary>
    [Fact]
    public async Task Post_Evaluate_ShuffledInputs_ProducesIdenticalOutput()
    {
        // Arrange - Create fixed test data with explicit versions
        var projects = new[]
        {
            new ProjectDto { Id = "Project-1", Name = "Project 1" },
            new ProjectDto { Id = "Project-2", Name = "Project 2" }
        };
        
        var environments = new[]
        {
            new EnvironmentDto { Id = "Environment-1", Name = "Env 1" },
            new EnvironmentDto { Id = "Environment-2", Name = "Env 2" }
        };
        
        var releases = new[]
        {
            new ReleaseDto { Id = "Release-1", ProjectId = "Project-1", Version = "1.0.0", Created = DateTimeOffset.Parse("2024-01-01T10:00:00Z") },
            new ReleaseDto { Id = "Release-2", ProjectId = "Project-2", Version = "2.0.0", Created = DateTimeOffset.Parse("2024-01-01T11:00:00Z") },
            new ReleaseDto { Id = "Release-3", ProjectId = "Project-1", Version = "1.1.0", Created = DateTimeOffset.Parse("2024-01-01T12:00:00Z") }
        };
        
        var deployments = new[]
        {
            new DeploymentDto { Id = "Deployment-1", ReleaseId = "Release-1", EnvironmentId = "Environment-1", DeployedAt = DateTimeOffset.Parse("2024-01-02T10:00:00Z") },
            new DeploymentDto { Id = "Deployment-2", ReleaseId = "Release-2", EnvironmentId = "Environment-2", DeployedAt = DateTimeOffset.Parse("2024-01-02T11:00:00Z") },
            new DeploymentDto { Id = "Deployment-3", ReleaseId = "Release-3", EnvironmentId = "Environment-2", DeployedAt = DateTimeOffset.Parse("2024-01-02T12:00:00Z") }
        };

        // Original order
        var requestOriginal = new EvaluateRetentionRequest
        {
            Dataset = new DatasetDto
            {
                Projects = projects,
                Environments = environments,
                Releases = releases,
                Deployments = deployments
            },
            ReleasesToKeep = 10
        };

        // Shuffled order - same data, reversed arrays
        var requestShuffled = new EvaluateRetentionRequest
        {
            Dataset = new DatasetDto
            {
                Projects = projects.Reverse().ToArray(),
                Environments = environments.Reverse().ToArray(),
                Releases = releases.Reverse().ToArray(),
                Deployments = deployments.Reverse().ToArray()
            },
            ReleasesToKeep = 10
        };

        // Act
        var responseOriginal = await _client.PostAsJsonAsync("/api/v1/retention/evaluate", requestOriginal, JsonOptions);
        var responseShuffled = await _client.PostAsJsonAsync("/api/v1/retention/evaluate", requestShuffled, JsonOptions);

        // Assert - Both should succeed
        responseOriginal.StatusCode.Should().Be(HttpStatusCode.OK);
        responseShuffled.StatusCode.Should().Be(HttpStatusCode.OK);

        // Normalize and compare JSON output
        var jsonOriginal = await GetNormalizedJson(responseOriginal);
        var jsonShuffled = await GetNormalizedJson(responseShuffled);

        jsonOriginal.Should().Be(jsonShuffled, "shuffled inputs must produce identical output");
    }

    /// <summary>
    /// Verifies that keptReleases are ordered by ProjectId, EnvironmentId, Rank.
    /// </summary>
    [Fact]
    public async Task Post_Evaluate_KeptReleasesAreDeterministicallySorted()
    {
        // Arrange - Create data that would need sorting
        var request = new EvaluateRetentionRequest
        {
            Dataset = new DatasetDto
            {
                Projects = new[]
                {
                    new ProjectDto { Id = "Project-B", Name = "Project B" },
                    new ProjectDto { Id = "Project-A", Name = "Project A" }
                },
                Environments = new[]
                {
                    new EnvironmentDto { Id = "Environment-2", Name = "Staging" },
                    new EnvironmentDto { Id = "Environment-1", Name = "Production" }
                },
                Releases = new[]
                {
                    new ReleaseDto { Id = "Release-B", ProjectId = "Project-B", Version = "1.0.0", Created = DateTimeOffset.Parse("2024-01-01T10:00:00Z") },
                    new ReleaseDto { Id = "Release-A", ProjectId = "Project-A", Version = "1.0.0", Created = DateTimeOffset.Parse("2024-01-01T10:00:00Z") }
                },
                Deployments = new[]
                {
                    new DeploymentDto { Id = "Deployment-B2", ReleaseId = "Release-B", EnvironmentId = "Environment-2", DeployedAt = DateTimeOffset.Parse("2024-01-02T10:00:00Z") },
                    new DeploymentDto { Id = "Deployment-A1", ReleaseId = "Release-A", EnvironmentId = "Environment-1", DeployedAt = DateTimeOffset.Parse("2024-01-02T10:00:00Z") },
                    new DeploymentDto { Id = "Deployment-B1", ReleaseId = "Release-B", EnvironmentId = "Environment-1", DeployedAt = DateTimeOffset.Parse("2024-01-02T10:00:00Z") },
                    new DeploymentDto { Id = "Deployment-A2", ReleaseId = "Release-A", EnvironmentId = "Environment-2", DeployedAt = DateTimeOffset.Parse("2024-01-02T10:00:00Z") }
                }
            },
            ReleasesToKeep = 10
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/v1/retention/evaluate", request, JsonOptions);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<EvaluateRetentionResponse>(JsonOptions);
        result.Should().NotBeNull();

        // Verify sorting: ProjectId asc, EnvironmentId asc, Rank asc
        var keptReleases = result!.KeptReleases;
        keptReleases.Should().HaveCount(4);

        // Expected order:
        // Project-A, Environment-1 (first)
        // Project-A, Environment-2 (second)
        // Project-B, Environment-1 (third)
        // Project-B, Environment-2 (fourth)
        keptReleases[0].ProjectId.Should().Be("Project-A");
        keptReleases[0].EnvironmentId.Should().Be("Environment-1");

        keptReleases[1].ProjectId.Should().Be("Project-A");
        keptReleases[1].EnvironmentId.Should().Be("Environment-2");

        keptReleases[2].ProjectId.Should().Be("Project-B");
        keptReleases[2].EnvironmentId.Should().Be("Environment-1");

        keptReleases[3].ProjectId.Should().Be("Project-B");
        keptReleases[3].EnvironmentId.Should().Be("Environment-2");
    }

    /// <summary>
    /// Verifies that multiple runs produce identical output (idempotency).
    /// </summary>
    [Fact]
    public async Task Post_Evaluate_MultipleRuns_ProduceIdenticalOutput()
    {
        // Arrange
        var request = new EvaluateRetentionRequest
        {
            Dataset = new DatasetDto
            {
                Projects = new[] { new ProjectDto { Id = "Project-1", Name = "Project 1" } },
                Environments = new[] { new EnvironmentDto { Id = "Environment-1", Name = "Prod" } },
                Releases = new[]
                {
                    new ReleaseDto { Id = "Release-1", ProjectId = "Project-1", Version = "1.0.0", Created = DateTimeOffset.Parse("2024-01-01T10:00:00Z") },
                    new ReleaseDto { Id = "Release-2", ProjectId = "Project-1", Version = "1.0.1", Created = DateTimeOffset.Parse("2024-01-01T11:00:00Z") }
                },
                Deployments = new[]
                {
                    new DeploymentDto { Id = "Deployment-1", ReleaseId = "Release-1", EnvironmentId = "Environment-1", DeployedAt = DateTimeOffset.Parse("2024-01-02T10:00:00Z") },
                    new DeploymentDto { Id = "Deployment-2", ReleaseId = "Release-2", EnvironmentId = "Environment-1", DeployedAt = DateTimeOffset.Parse("2024-01-02T11:00:00Z") }
                }
            },
            ReleasesToKeep = 10
        };

        // Act - Run multiple times
        var responses = new List<string>();
        for (int i = 0; i < 5; i++)
        {
            var response = await _client.PostAsJsonAsync("/api/v1/retention/evaluate", request, JsonOptions);
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            responses.Add(await GetNormalizedJson(response));
        }

        // Assert - All runs should produce identical output
        responses.Distinct().Should().HaveCount(1, "all runs must produce identical output");
    }

    private static async Task<string> GetNormalizedJson(HttpResponseMessage response)
    {
        var result = await response.Content.ReadFromJsonAsync<EvaluateRetentionResponse>(JsonOptions);
        // Remove correlation_id from comparison as it may vary
        var normalized = result! with { CorrelationId = null };
        return JsonSerializer.Serialize(normalized, JsonOptions);
    }
}
