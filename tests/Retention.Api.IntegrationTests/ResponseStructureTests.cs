using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Retention.Api.Contracts;

namespace Retention.Api.IntegrationTests;

/// <summary>
/// Integration tests for response structures and decision log accuracy.
/// </summary>
public class ResponseStructureTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client;
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true
    };

    public ResponseStructureTests(WebApplicationFactory<Program> factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Evaluate_ResponseContainsKeptReleases()
    {
        // Arrange
        var request = new EvaluateRetentionRequest
        {
            Dataset = new DatasetDto
            {
                Projects = new[] { new ProjectDto { Id = "Proj-1", Name = "Project 1" } },
                Environments = new[] { new EnvironmentDto { Id = "Env-1", Name = "Prod" } },
                Releases = new[]
                {
                    new ReleaseDto { Id = "R1", ProjectId = "Proj-1", Version = "1.0.0", Created = DateTimeOffset.Parse("2024-01-01T10:00:00Z") }
                },
                Deployments = new[]
                {
                    new DeploymentDto { Id = "D1", ReleaseId = "R1", EnvironmentId = "Env-1", DeployedAt = DateTimeOffset.Parse("2024-01-01T11:00:00Z") }
                }
            },
            ReleasesToKeep = 1
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/v1/retention/evaluate", request, JsonOptions);
        var result = await response.Content.ReadFromJsonAsync<EvaluateRetentionResponse>(JsonOptions);

        // Assert
        result.Should().NotBeNull();
        result!.KeptReleases.Should().NotBeNull();
        result.KeptReleases.Should().HaveCount(1);
        result.KeptReleases[0].ReleaseId.Should().Be("R1");
        result.KeptReleases[0].ProjectId.Should().Be("Proj-1");
        result.KeptReleases[0].EnvironmentId.Should().Be("Env-1");
        result.KeptReleases[0].Version.Should().Be("1.0.0");
        result.KeptReleases[0].Rank.Should().Be(1);
    }

    [Fact]
    public async Task Evaluate_ResponseContainsDecisions()
    {
        // Arrange
        var request = new EvaluateRetentionRequest
        {
            Dataset = new DatasetDto
            {
                Projects = new[] { new ProjectDto { Id = "Proj-1", Name = "Project 1" } },
                Environments = new[] { new EnvironmentDto { Id = "Env-1", Name = "Prod" } },
                Releases = new[]
                {
                    new ReleaseDto { Id = "R1", ProjectId = "Proj-1", Version = "1.0.0", Created = DateTimeOffset.Parse("2024-01-01T10:00:00Z") }
                },
                Deployments = new[]
                {
                    new DeploymentDto { Id = "D1", ReleaseId = "R1", EnvironmentId = "Env-1", DeployedAt = DateTimeOffset.Parse("2024-01-01T11:00:00Z") }
                }
            },
            ReleasesToKeep = 1
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/v1/retention/evaluate", request, JsonOptions);
        var result = await response.Content.ReadFromJsonAsync<EvaluateRetentionResponse>(JsonOptions);

        // Assert
        result.Should().NotBeNull();
        result!.Decisions.Should().NotBeNull();
        result.Decisions.Should().NotBeEmpty();
        var keptDecision = result.Decisions.FirstOrDefault(d => d.ReleaseId == "R1");
        keptDecision.Should().NotBeNull();
        keptDecision!.ReasonCode.Should().Be("kept.top_n");
    }

    [Fact]
    public async Task Evaluate_ResponseContainsDiagnostics()
    {
        // Arrange
        var request = new EvaluateRetentionRequest
        {
            Dataset = new DatasetDto
            {
                Projects = new[] { new ProjectDto { Id = "Proj-1", Name = "Project 1" } },
                Environments = new[] { new EnvironmentDto { Id = "Env-1", Name = "Prod" } },
                Releases = new[]
                {
                    new ReleaseDto { Id = "R1", ProjectId = "Proj-1", Version = "1.0.0", Created = DateTimeOffset.Parse("2024-01-01T10:00:00Z") }
                },
                Deployments = new[]
                {
                    new DeploymentDto { Id = "D1", ReleaseId = "R1", EnvironmentId = "Env-1", DeployedAt = DateTimeOffset.Parse("2024-01-01T11:00:00Z") }
                }
            },
            ReleasesToKeep = 1
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/v1/retention/evaluate", request, JsonOptions);
        var result = await response.Content.ReadFromJsonAsync<EvaluateRetentionResponse>(JsonOptions);

        // Assert
        result.Should().NotBeNull();
        result!.Diagnostics.Should().NotBeNull();
        result.Diagnostics.GroupsEvaluated.Should().BeGreaterThan(0);
        result.Diagnostics.TotalKeptReleases.Should().Be(1);
        result.Diagnostics.InvalidDeploymentsExcluded.Should().Be(0);
    }

    [Fact]
    public async Task Evaluate_WithInvalidReferenceExcludesDeployment()
    {
        // Arrange
        var request = new EvaluateRetentionRequest
        {
            Dataset = new DatasetDto
            {
                Projects = new[] { new ProjectDto { Id = "Proj-1", Name = "Project 1" } },
                Environments = new[] { new EnvironmentDto { Id = "Env-1", Name = "Prod" } },
                Releases = new[]
                {
                    new ReleaseDto { Id = "R1", ProjectId = "Proj-1", Version = "1.0.0", Created = DateTimeOffset.Parse("2024-01-01T10:00:00Z") }
                },
                Deployments = new[]
                {
                    // Valid deployment
                    new DeploymentDto { Id = "D1", ReleaseId = "R1", EnvironmentId = "Env-1", DeployedAt = DateTimeOffset.Parse("2024-01-01T11:00:00Z") },
                    // Invalid deployment (missing environment)
                    new DeploymentDto { Id = "D2", ReleaseId = "R1", EnvironmentId = "Env-Missing", DeployedAt = DateTimeOffset.Parse("2024-01-01T12:00:00Z") }
                }
            },
            ReleasesToKeep = 1
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/v1/retention/evaluate", request, JsonOptions);
        var result = await response.Content.ReadFromJsonAsync<EvaluateRetentionResponse>(JsonOptions);

        // Assert
        result.Should().NotBeNull();
        result!.Diagnostics.InvalidDeploymentsExcluded.Should().Be(1);
        var diagnosticDecisions = result.Decisions.Where(d => d.ReasonCode == "diagnostic.invalid_reference");
        diagnosticDecisions.Should().NotBeEmpty();
    }

    [Fact]
    public async Task Evaluate_WithMissingReleaseReference_ExcludesDeployment()
    {
        // Arrange
        var request = new EvaluateRetentionRequest
        {
            Dataset = new DatasetDto
            {
                Projects = new[] { new ProjectDto { Id = "Proj-1", Name = "Project 1" } },
                Environments = new[] { new EnvironmentDto { Id = "Env-1", Name = "Prod" } },
                Releases = new[]
                {
                    new ReleaseDto { Id = "R1", ProjectId = "Proj-1", Version = "1.0.0", Created = DateTimeOffset.Parse("2024-01-01T10:00:00Z") }
                },
                Deployments = new[]
                {
                    // Deployment with missing release
                    new DeploymentDto { Id = "D1", ReleaseId = "R-Missing", EnvironmentId = "Env-1", DeployedAt = DateTimeOffset.Parse("2024-01-01T11:00:00Z") }
                }
            },
            ReleasesToKeep = 1
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/v1/retention/evaluate", request, JsonOptions);
        var result = await response.Content.ReadFromJsonAsync<EvaluateRetentionResponse>(JsonOptions);

        // Assert
        result.Should().NotBeNull();
        result!.Diagnostics.InvalidDeploymentsExcluded.Should().Be(1);
    }

    [Fact]
    public async Task Validate_ResponseContainsIsValidFlag()
    {
        // Arrange
        var request = new ValidateDatasetRequest
        {
            Dataset = new DatasetDto
            {
                Projects = new[] { new ProjectDto { Id = "Proj-1", Name = "Project 1" } },
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
        result!.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task Validate_ResponseContainsErrorsList()
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
        result!.Errors.Should().NotBeNull();
        result.Errors.Should().NotBeEmpty();
        result.Errors.Should().Contain(e => e.Code == "validation.duplicate_project_id");
    }

    [Fact]
    public async Task Evaluate_WithMultipleReleasesSameEnvironment_RanksCorrectly()
    {
        // Arrange
        var request = new EvaluateRetentionRequest
        {
            Dataset = new DatasetDto
            {
                Projects = new[] { new ProjectDto { Id = "Proj-1", Name = "Project 1" } },
                Environments = new[] { new EnvironmentDto { Id = "Env-1", Name = "Prod" } },
                Releases = new[]
                {
                    new ReleaseDto { Id = "R1", ProjectId = "Proj-1", Version = "1.0.0", Created = DateTimeOffset.Parse("2024-01-01T10:00:00Z") },
                    new ReleaseDto { Id = "R2", ProjectId = "Proj-1", Version = "1.0.1", Created = DateTimeOffset.Parse("2024-01-01T11:00:00Z") },
                    new ReleaseDto { Id = "R3", ProjectId = "Proj-1", Version = "1.0.2", Created = DateTimeOffset.Parse("2024-01-01T12:00:00Z") }
                },
                Deployments = new[]
                {
                    new DeploymentDto { Id = "D1", ReleaseId = "R1", EnvironmentId = "Env-1", DeployedAt = DateTimeOffset.Parse("2024-01-01T10:30:00Z") },
                    new DeploymentDto { Id = "D2", ReleaseId = "R2", EnvironmentId = "Env-1", DeployedAt = DateTimeOffset.Parse("2024-01-01T11:30:00Z") },
                    new DeploymentDto { Id = "D3", ReleaseId = "R3", EnvironmentId = "Env-1", DeployedAt = DateTimeOffset.Parse("2024-01-01T12:30:00Z") }
                }
            },
            ReleasesToKeep = 2
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/v1/retention/evaluate", request, JsonOptions);
        var result = await response.Content.ReadFromJsonAsync<EvaluateRetentionResponse>(JsonOptions);

        // Assert
        result.Should().NotBeNull();
        result!.KeptReleases.Should().HaveCount(2);
        result.KeptReleases.Should().Satisfy(
            r1 => r1.ReleaseId == "R3" && r1.Rank == 1,
            r2 => r2.ReleaseId == "R2" && r2.Rank == 2
        );
    }

    [Fact]
    public async Task Evaluate_RanksAreDeterministic()
    {
        // Arrange
        var request = new EvaluateRetentionRequest
        {
            Dataset = new DatasetDto
            {
                Projects = new[] { new ProjectDto { Id = "Proj-1", Name = "Project 1" } },
                Environments = new[] { new EnvironmentDto { Id = "Env-1", Name = "Prod" } },
                Releases = new[]
                {
                    new ReleaseDto { Id = "R1", ProjectId = "Proj-1", Version = "1.0.0", Created = DateTimeOffset.Parse("2024-01-01T10:00:00Z") },
                    new ReleaseDto { Id = "R2", ProjectId = "Proj-1", Version = "1.0.1", Created = DateTimeOffset.Parse("2024-01-01T11:00:00Z") }
                },
                Deployments = new[]
                {
                    new DeploymentDto { Id = "D1", ReleaseId = "R1", EnvironmentId = "Env-1", DeployedAt = DateTimeOffset.Parse("2024-01-01T11:00:00Z") },
                    new DeploymentDto { Id = "D2", ReleaseId = "R2", EnvironmentId = "Env-1", DeployedAt = DateTimeOffset.Parse("2024-01-01T12:00:00Z") }
                }
            },
            ReleasesToKeep = 1
        };

        // Act - Run twice
        var response1 = await _client.PostAsJsonAsync("/api/v1/retention/evaluate", request, JsonOptions);
        var result1 = await response1.Content.ReadFromJsonAsync<EvaluateRetentionResponse>(JsonOptions);

        var response2 = await _client.PostAsJsonAsync("/api/v1/retention/evaluate", request, JsonOptions);
        var result2 = await response2.Content.ReadFromJsonAsync<EvaluateRetentionResponse>(JsonOptions);

        // Assert - Should get identical results
        result1.Should().NotBeNull();
        result2.Should().NotBeNull();
        result1!.KeptReleases.Should().Equal(result2!.KeptReleases);
    }
}
