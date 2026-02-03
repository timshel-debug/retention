using System.Text.Json.Serialization;

namespace Retention.Api.Contracts;

/// <summary>
/// Request for validating a dataset.
/// </summary>
public sealed record ValidateDatasetRequest
{
    [JsonPropertyName("dataset")]
    public required DatasetDto Dataset { get; init; }
    
    [JsonPropertyName("correlationId")]
    public string? CorrelationId { get; init; }
}

/// <summary>
/// Response from dataset validation.
/// </summary>
public sealed record ValidateDatasetResponse
{
    [JsonPropertyName("isValid")]
    public required bool IsValid { get; init; }
    
    [JsonPropertyName("errors")]
    public required ValidationMessageDto[] Errors { get; init; }
    
    [JsonPropertyName("warnings")]
    public required ValidationMessageDto[] Warnings { get; init; }
    
    [JsonPropertyName("summary")]
    public required ValidationSummaryDto Summary { get; init; }
}

public sealed record ValidationMessageDto
{
    [JsonPropertyName("code")]
    public required string Code { get; init; }
    
    [JsonPropertyName("message")]
    public required string Message { get; init; }
    
    [JsonPropertyName("path")]
    public string? Path { get; init; }
}

public sealed record ValidationSummaryDto
{
    [JsonPropertyName("projectCount")]
    public required int ProjectCount { get; init; }
    
    [JsonPropertyName("environmentCount")]
    public required int EnvironmentCount { get; init; }
    
    [JsonPropertyName("releaseCount")]
    public required int ReleaseCount { get; init; }
    
    [JsonPropertyName("deploymentCount")]
    public required int DeploymentCount { get; init; }
    
    [JsonPropertyName("errorCount")]
    public required int ErrorCount { get; init; }
    
    [JsonPropertyName("warningCount")]
    public required int WarningCount { get; init; }
}
