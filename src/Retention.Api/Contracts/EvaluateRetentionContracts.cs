using System.Text.Json.Serialization;

namespace Retention.Api.Contracts;

/// <summary>
/// Request for evaluating retention policy.
/// </summary>
public sealed record EvaluateRetentionRequest
{
    [JsonPropertyName("dataset")]
    public required DatasetDto Dataset { get; init; }
    
    [JsonPropertyName("releasesToKeep")]
    public required int ReleasesToKeep { get; init; }
    
    [JsonPropertyName("correlationId")]
    public string? CorrelationId { get; init; }
}

/// <summary>
/// Response from retention evaluation containing kept releases, decisions, and diagnostics.
/// </summary>
public sealed record EvaluateRetentionResponse
{
    [JsonPropertyName("keptReleases")]
    public required KeptReleaseDto[] KeptReleases { get; init; }
    
    [JsonPropertyName("decisions")]
    public required DecisionDto[] Decisions { get; init; }
    
    [JsonPropertyName("diagnostics")]
    public required DiagnosticsDto Diagnostics { get; init; }
    
    [JsonPropertyName("correlationId")]
    public string? CorrelationId { get; init; }
}

public sealed record KeptReleaseDto
{
    [JsonPropertyName("releaseId")]
    public required string ReleaseId { get; init; }
    
    [JsonPropertyName("projectId")]
    public required string ProjectId { get; init; }
    
    [JsonPropertyName("environmentId")]
    public required string EnvironmentId { get; init; }
    
    [JsonPropertyName("version")]
    public string? Version { get; init; }
    
    [JsonPropertyName("created")]
    public required DateTimeOffset Created { get; init; }
    
    [JsonPropertyName("latestDeployedAt")]
    public required DateTimeOffset LatestDeployedAt { get; init; }
    
    [JsonPropertyName("rank")]
    public required int Rank { get; init; }
    
    [JsonPropertyName("reasonCode")]
    public required string ReasonCode { get; init; }
}

public sealed record DecisionDto
{
    [JsonPropertyName("projectId")]
    public required string ProjectId { get; init; }
    
    [JsonPropertyName("environmentId")]
    public required string EnvironmentId { get; init; }
    
    [JsonPropertyName("releaseId")]
    public required string ReleaseId { get; init; }
    
    [JsonPropertyName("n")]
    public required int N { get; init; }
    
    [JsonPropertyName("rank")]
    public required int Rank { get; init; }
    
    [JsonPropertyName("latestDeployedAt")]
    public DateTimeOffset? LatestDeployedAt { get; init; }
    
    [JsonPropertyName("reasonCode")]
    public required string ReasonCode { get; init; }
    
    [JsonPropertyName("reasonText")]
    public required string ReasonText { get; init; }
}

public sealed record DiagnosticsDto
{
    [JsonPropertyName("groupsEvaluated")]
    public required int GroupsEvaluated { get; init; }
    
    [JsonPropertyName("invalidDeploymentsExcluded")]
    public required int InvalidDeploymentsExcluded { get; init; }
    
    [JsonPropertyName("totalKeptReleases")]
    public required int TotalKeptReleases { get; init; }
}
