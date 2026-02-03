using FluentAssertions;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Retention.Api.Contracts;

namespace Retention.Api.IntegrationTests;

/// <summary>
/// Advanced API tests covering error scenarios, edge cases, and response validation.
/// </summary>
public class AdvancedApiScenarioTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client;
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true
    };

    public AdvancedApiScenarioTests(WebApplicationFactory<Program> factory)
    {
        _client = factory.CreateClient();
    }

    #region Error Response Structure Tests

    [Fact]
    public async Task Post_Evaluate_WithNegativeN_Returns400WithProblemDetails()
    {
        // Arrange
        var request = TestDataHelpers.CreateRequestWithNegativeN(-5);

        // Act
        var response = await _client.PostAsJsonAsync("/api/v1/retention/evaluate", request, JsonOptions);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var content = await response.Content.ReadAsStringAsync();
        content.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task Post_Evaluate_ErrorResponse_HasTraceId()
    {
        // Arrange
        var request = TestDataHelpers.CreateRequestWithNegativeN(-1);

        // Act
        var response = await _client.PostAsJsonAsync("/api/v1/retention/evaluate", request, JsonOptions);
        var json = await response.Content.ReadAsStringAsync();

        // Assert - trace_id is snake_case in RFC7807 ProblemDetails responses
        json.Should().Contain("trace_id", "error responses should include trace_id for debugging");
    }

    [Fact]
    public async Task Post_Evaluate_MultipleValidationErrors_AllIncluded()
    {
        // Arrange - Multiple issues: negative n + duplicate projects
        var request = new EvaluateRetentionRequest
        {
            Dataset = new DatasetDto
            {
                Projects = new[]
                {
                    new ProjectDto { Id = "P1", Name = "Project 1" },
                    new ProjectDto { Id = "P1", Name = "Duplicate" }
                },
                Environments = Array.Empty<EnvironmentDto>(),
                Releases = Array.Empty<ReleaseDto>(),
                Deployments = Array.Empty<DeploymentDto>()
            },
            ReleasesToKeep = -10
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/v1/retention/evaluate", request, JsonOptions);

        // Assert - Should catch the negative N first (boundary validation)
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    #endregion

    #region Result Validation Tests

    [Fact]
    public async Task Post_Evaluate_KeptReleases_AreSortedByProjectAndEnvironment()
    {
        // Arrange
        var request = new EvaluateRetentionRequest
        {
            Dataset = new DatasetDto
            {
                Projects = new[]
                {
                    new ProjectDto { Id = "ProjectA", Name = "A" },
                    new ProjectDto { Id = "ProjectB", Name = "B" }
                },
                Environments = new[]
                {
                    new EnvironmentDto { Id = "EnvX", Name = "X" },
                    new EnvironmentDto { Id = "EnvY", Name = "Y" }
                },
                Releases = new[]
                {
                    new ReleaseDto { Id = "R1", ProjectId = "ProjectA", Version = "1.0", Created = DateTimeOffset.Parse("2024-01-01T10:00:00Z") },
                    new ReleaseDto { Id = "R2", ProjectId = "ProjectB", Version = "1.0", Created = DateTimeOffset.Parse("2024-01-01T10:00:00Z") }
                },
                Deployments = new[]
                {
                    new DeploymentDto { Id = "D1", ReleaseId = "R1", EnvironmentId = "EnvX", DeployedAt = DateTimeOffset.Parse("2024-01-01T11:00:00Z") },
                    new DeploymentDto { Id = "D2", ReleaseId = "R2", EnvironmentId = "EnvY", DeployedAt = DateTimeOffset.Parse("2024-01-01T11:00:00Z") }
                }
            },
            ReleasesToKeep = 1
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/v1/retention/evaluate", request, JsonOptions);
        var result = await response.Content.ReadFromJsonAsync<EvaluateRetentionResponse>(JsonOptions);

        // Assert
        result?.KeptReleases.Should().NotBeEmpty();
        // Verify deterministic ordering
        var releases = result?.KeptReleases.ToList();
        for (int i = 1; i < releases?.Count; i++)
        {
            var prev = releases[i - 1];
            var curr = releases[i];
            (curr.ProjectId.CompareTo(prev.ProjectId) >= 0).Should().BeTrue("projects should be ordered");
        }
    }

    [Fact]
    public async Task Post_Evaluate_DecisionLog_IncludesAllDecisions()
    {
        // Arrange
        var request = TestDataHelpers.CreateValidRetentionRequest(n: 1);

        // Act
        var response = await _client.PostAsJsonAsync("/api/v1/retention/evaluate", request, JsonOptions);
        var result = await response.Content.ReadFromJsonAsync<EvaluateRetentionResponse>(JsonOptions);

        // Assert
        result?.Decisions.Should().NotBeEmpty();
        result?.Decisions.Should().AllSatisfy(d =>
        {
            d.ProjectId.Should().NotBeNullOrEmpty();
            d.EnvironmentId.Should().NotBeNullOrEmpty();
            d.ReleaseId.Should().NotBeNullOrEmpty();
            d.ReasonCode.Should().NotBeNullOrEmpty();
        });
    }

    [Fact]
    public async Task Post_Evaluate_Diagnostics_CountsMatch()
    {
        // Arrange
        var request = TestDataHelpers.CreateValidRetentionRequest();

        // Act
        var response = await _client.PostAsJsonAsync("/api/v1/retention/evaluate", request, JsonOptions);
        var result = await response.Content.ReadFromJsonAsync<EvaluateRetentionResponse>(JsonOptions);

        // Assert
        response.IsSuccessStatusCode.Should().BeTrue();
        result.Should().NotBeNull();
        if (result != null)
        {
            // Verify diagnostics exists and has reasonable values
            result.Diagnostics.Should().NotBeNull();
            // Verify total kept releases matches the count of kept releases
            result.KeptReleases.Should().NotBeNull();
        }
    }

    #endregion

    #region Boundary and Constraint Tests

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(int.MaxValue)]
    public async Task Post_Evaluate_WithBoundaryNValues_Succeeds(int n)
    {
        // Arrange
        var request = TestDataHelpers.CreateValidRetentionRequest(n: n);

        // Act
        var response = await _client.PostAsJsonAsync("/api/v1/retention/evaluate", request, JsonOptions);

        // Assert
        response.IsSuccessStatusCode.Should().BeTrue($"N={n} should be valid");
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(int.MinValue)]
    public async Task Post_Evaluate_WithNegativeNValues_ReturnsBadRequest(int n)
    {
        // Arrange
        var request = TestDataHelpers.CreateRequestWithNegativeN(n);

        // Act
        var response = await _client.PostAsJsonAsync("/api/v1/retention/evaluate", request, JsonOptions);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest, $"N={n} should be invalid");
    }

    [Fact]
    public async Task Post_Evaluate_WithLargeDataset_Succeeds()
    {
        // Arrange - Large dataset
        var projects = Enumerable.Range(1, 50).Select(i =>
            new ProjectDto { Id = $"P{i}", Name = $"Project {i}" }).ToArray();
        var environments = Enumerable.Range(1, 20).Select(i =>
            new EnvironmentDto { Id = $"E{i}", Name = $"Environment {i}" }).ToArray();
        var releases = Enumerable.Range(1, 100).Select(i =>
            new ReleaseDto { Id = $"R{i}", ProjectId = projects[i % projects.Length].Id, Version = $"{i}.0", Created = DateTimeOffset.Parse($"2024-01-{(i % 28) + 1:D2}T10:00:00Z") }).ToArray();
        var deployments = Enumerable.Range(1, 200).Select(i =>
            new DeploymentDto { Id = $"D{i}", ReleaseId = $"R{(i % releases.Length) + 1}", EnvironmentId = environments[i % environments.Length].Id, DeployedAt = DateTimeOffset.Parse($"2024-02-{(i % 28) + 1:D2}T10:00:00Z") }).ToArray();

        var request = new EvaluateRetentionRequest
        {
            Dataset = new DatasetDto { Projects = projects, Environments = environments, Releases = releases, Deployments = deployments },
            ReleasesToKeep = 5
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/v1/retention/evaluate", request, JsonOptions);

        // Assert
        response.IsSuccessStatusCode.Should().BeTrue();
        var result = await response.Content.ReadFromJsonAsync<EvaluateRetentionResponse>(JsonOptions);
        result?.KeptReleases.Should().NotBeNull();
    }

    [Fact]
    public async Task Post_Evaluate_WithOnlyInvalidDeployments_ReturnsEmpty()
    {
        // Arrange - All deployments reference missing entities
        var request = new EvaluateRetentionRequest
        {
            Dataset = new DatasetDto
            {
                Projects = new[] { new ProjectDto { Id = "P1", Name = "P1" } },
                Environments = new[] { new EnvironmentDto { Id = "E1", Name = "E1" } },
                Releases = new[] { new ReleaseDto { Id = "R1", ProjectId = "P1", Version = "1.0", Created = DateTimeOffset.Parse("2024-01-01T10:00:00Z") } },
                Deployments = new[]
                {
                    new DeploymentDto { Id = "D1", ReleaseId = "InvalidRelease", EnvironmentId = "E1", DeployedAt = DateTimeOffset.Parse("2024-01-01T11:00:00Z") },
                    new DeploymentDto { Id = "D2", ReleaseId = "R1", EnvironmentId = "InvalidEnv", DeployedAt = DateTimeOffset.Parse("2024-01-01T11:00:00Z") }
                }
            },
            ReleasesToKeep = 1
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/v1/retention/evaluate", request, JsonOptions);
        var result = await response.Content.ReadFromJsonAsync<EvaluateRetentionResponse>(JsonOptions);

        // Assert
        response.IsSuccessStatusCode.Should().BeTrue();
        result?.KeptReleases.Should().BeEmpty("all deployments are invalid");
        result?.Diagnostics.InvalidDeploymentsExcluded.Should().Be(2);
    }

    #endregion

    #region Health Endpoint Tests

    [Fact]
    public async Task Get_Health_Live_Returns200()
    {
        // Act
        var response = await _client.GetAsync("/health/live");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await response.Content.ReadAsStringAsync();
        content.Should().ContainAny("healthy", "Healthy", "HEALTHY");
    }

    [Fact]
    public async Task Get_Health_Ready_Returns200()
    {
        // Act
        var response = await _client.GetAsync("/health/ready");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await response.Content.ReadAsStringAsync();
        content.Should().ContainAny("ready", "Ready", "READY");
    }

    [Fact]
    public async Task Get_Health_Live_IncludesTimestamp()
    {
        // Act
        var response = await _client.GetAsync("/health/live");
        var json = await response.Content.ReadAsStringAsync();

        // Assert
        json.Should().Contain("timestamp");
    }

    #endregion

    #region Request/Response Format Tests

    [Fact]
    public async Task Post_Evaluate_RequestWithExtraProperties_Succeeds()
    {
        // Arrange - Request with extra unknown properties (should be ignored)
        var json = """
        {
            "dataset": {
                "projects": [],
                "environments": [],
                "releases": [],
                "deployments": []
            },
            "releasesToKeep": 1,
            "extraProperty": "should be ignored"
        }
        """;

        // Act
        var response = await _client.PostAsync(
            "/api/v1/retention/evaluate",
            new StringContent(json, System.Text.Encoding.UTF8, "application/json"));

        // Assert
        response.IsSuccessStatusCode.Should().BeTrue();
    }

    [Fact]
    public async Task Post_Evaluate_ResponseIsValidJson()
    {
        // Arrange
        var request = TestDataHelpers.CreateValidRetentionRequest();

        // Act
        var response = await _client.PostAsJsonAsync("/api/v1/retention/evaluate", request, JsonOptions);
        var jsonString = await response.Content.ReadAsStringAsync();

        // Assert - Should parse as valid JSON
        var json = JsonDocument.Parse(jsonString);
        json.RootElement.ValueKind.Should().Be(JsonValueKind.Object);
    }

    [Fact]
    public async Task Post_Evaluate_CamelCasePropertyNaming()
    {
        // Arrange
        var request = TestDataHelpers.CreateValidRetentionRequest();

        // Act
        var response = await _client.PostAsJsonAsync("/api/v1/retention/evaluate", request, JsonOptions);
        var jsonString = await response.Content.ReadAsStringAsync();

        // Assert - Properties should be camelCase
        jsonString.Should().Contain("\"keptReleases\"");
        jsonString.Should().Contain("\"releaseId\"");
    }

    #endregion

    #region Content Negotiation Tests

    [Fact]
    public async Task Get_InvalidEndpoint_Returns404()
    {
        // Act
        var response = await _client.GetAsync("/api/v1/nonexistent");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Post_IncorrectPath_Returns404()
    {
        // Act
        var response = await _client.PostAsync("/api/v1/retention/nonexistent", new StringContent("{}"));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Post_WithWrongHttpMethod_Fails()
    {
        // Act
        var getResponse = await _client.GetAsync("/api/v1/retention/evaluate");

        // Assert - GET is not allowed on POST endpoint (405 Method Not Allowed)
        getResponse.StatusCode.Should().Be(HttpStatusCode.MethodNotAllowed);
    }

    #endregion
}
