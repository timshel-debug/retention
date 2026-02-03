using FluentAssertions;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Retention.Api.Contracts;

namespace Retention.Api.IntegrationTests;

/// <summary>
/// HTTP infrastructure and protocol-level tests for API layer.
/// Tests content negotiation, encoding, caching headers, and error responses.
/// </summary>
public class ApiHttpInfrastructureTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client;
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true
    };

    public ApiHttpInfrastructureTests(WebApplicationFactory<Program> factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Post_RetentionEvaluate_ReturnsJsonContentType()
    {
        // Arrange
        var payload = TestDataHelpers.CreateValidRetentionRequest();

        // Act
        var response = await _client.PostAsJsonAsync("/api/v1/retention/evaluate", payload, JsonOptions);

        // Assert
        response.Content.Headers.ContentType?.MediaType.Should().Be("application/json");
    }

    [Fact]
    public async Task Post_RetentionEvaluate_ValidRequest_Returns200Ok()
    {
        // Arrange
        var payload = TestDataHelpers.CreateValidRetentionRequest();

        // Act
        var response = await _client.PostAsJsonAsync("/api/v1/retention/evaluate", payload, JsonOptions);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Post_RetentionEvaluate_BadRequest_Returns400()
    {
        // Arrange - Negative n value
        var payload = TestDataHelpers.CreateRequestWithNegativeN(-1);

        // Act
        var response = await _client.PostAsJsonAsync("/api/v1/retention/evaluate", payload, JsonOptions);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Post_RetentionEvaluate_InvalidJson_Returns400BadRequest()
    {
        // Arrange
        var invalidJson = "{invalid json";

        // Act
        var response = await _client.PostAsync(
            "/api/v1/retention/evaluate",
            new StringContent(invalidJson, System.Text.Encoding.UTF8, "application/json"));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Post_RetentionEvaluate_EmptyBody_Returns400BadRequest()
    {
        // Arrange
        var content = new StringContent("", System.Text.Encoding.UTF8, "application/json");

        // Act
        var response = await _client.PostAsync("/api/v1/retention/evaluate", content);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Get_HealthLive_Returns200()
    {
        // Act
        var response = await _client.GetAsync("/health/live");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Get_HealthReady_Returns200()
    {
        // Act
        var response = await _client.GetAsync("/health/ready");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Get_InvalidEndpoint_Returns404NotFound()
    {
        // Act
        var response = await _client.GetAsync("/api/v1/nonexistent");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Post_RetentionEvaluate_WithCorrelationId_PreservesIt()
    {
        // Arrange
        var correlationId = "test-correlation-id-12345";
        var payload = TestDataHelpers.CreateValidRetentionRequest(correlationId: correlationId);

        // Act
        var response = await _client.PostAsJsonAsync("/api/v1/retention/evaluate", payload, JsonOptions);
        var result = await response.Content.ReadFromJsonAsync<EvaluateRetentionResponse>(JsonOptions);

        // Assert
        result?.CorrelationId.Should().Be(correlationId);
    }

    [Fact]
    public async Task Post_RetentionEvaluate_WithoutCorrelationId_ReturnsNull()
    {
        // Arrange
        var payload = TestDataHelpers.CreateValidRetentionRequest(correlationId: null);

        // Act
        var response = await _client.PostAsJsonAsync("/api/v1/retention/evaluate", payload, JsonOptions);
        var result = await response.Content.ReadFromJsonAsync<EvaluateRetentionResponse>(JsonOptions);

        // Assert
        result?.CorrelationId.Should().BeNull();
    }

    [Fact]
    public async Task Post_RetentionEvaluate_ResponseHasAllRequiredFields()
    {
        // Arrange
        var payload = TestDataHelpers.CreateValidRetentionRequest();

        // Act
        var response = await _client.PostAsJsonAsync("/api/v1/retention/evaluate", payload, JsonOptions);
        var result = await response.Content.ReadFromJsonAsync<EvaluateRetentionResponse>(JsonOptions);

        // Assert
        result.Should().NotBeNull();
        result?.KeptReleases.Should().NotBeNull();
        result?.Decisions.Should().NotBeNull();
        result?.Diagnostics.Should().NotBeNull();
    }

    [Fact]
    public async Task Post_RetentionEvaluate_ResponseContentIsJsonSerializable()
    {
        // Arrange
        var payload = TestDataHelpers.CreateValidRetentionRequest();

        // Act
        var response = await _client.PostAsJsonAsync("/api/v1/retention/evaluate", payload, JsonOptions);
        var json = await response.Content.ReadAsStringAsync();

        // Assert - Should be able to deserialize
        var doc = JsonDocument.Parse(json);
        doc.RootElement.ValueKind.Should().Be(JsonValueKind.Object);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(100)]
    public async Task Post_RetentionEvaluate_WithVariousNValues_Succeeds(int n)
    {
        // Arrange
        var payload = TestDataHelpers.CreateValidRetentionRequest(n: n);

        // Act
        var response = await _client.PostAsJsonAsync("/api/v1/retention/evaluate", payload, JsonOptions);

        // Assert
        response.IsSuccessStatusCode.Should().BeTrue();
    }

    [Fact]
    public async Task Post_RetentionEvaluate_WithVeryLargeN_Succeeds()
    {
        // Arrange
        var payload = TestDataHelpers.CreateValidRetentionRequest(n: int.MaxValue);

        // Act
        var response = await _client.PostAsJsonAsync("/api/v1/retention/evaluate", payload, JsonOptions);

        // Assert
        response.IsSuccessStatusCode.Should().BeTrue();
    }

    [Fact]
    public async Task Post_RetentionEvaluate_WithEmptyCollections_Succeeds()
    {
        // Arrange
        var payload = TestDataHelpers.CreateEmptyRequest();

        // Act
        var response = await _client.PostAsJsonAsync("/api/v1/retention/evaluate", payload, JsonOptions);

        // Assert
        response.IsSuccessStatusCode.Should().BeTrue();
    }

    [Fact]
    public async Task Post_RetentionEvaluate_AnyHttpMethodOtherThanPost_NotAllowed()
    {
        // Act
        var getResponse = await _client.GetAsync("/api/v1/retention/evaluate");

        // Assert - GET should not be allowed (either 404 or 405)
        (getResponse.StatusCode == HttpStatusCode.NotFound || getResponse.StatusCode == HttpStatusCode.MethodNotAllowed).Should().BeTrue();
    }
}

