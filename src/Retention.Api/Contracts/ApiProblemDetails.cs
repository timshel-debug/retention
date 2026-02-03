using System.Text.Json.Serialization;

namespace Retention.Api.Contracts;

/// <summary>
/// RFC7807 ProblemDetails extension with stable error codes and trace information.
/// </summary>
public sealed record ApiProblemDetails
{
    [JsonPropertyName("type")]
    public string? Type { get; init; }
    
    [JsonPropertyName("title")]
    public required string Title { get; init; }
    
    [JsonPropertyName("status")]
    public required int Status { get; init; }
    
    [JsonPropertyName("detail")]
    public string? Detail { get; init; }
    
    [JsonPropertyName("instance")]
    public string? Instance { get; init; }
    
    /// <summary>
    /// Stable error code for programmatic handling.
    /// </summary>
    [JsonPropertyName("error_code")]
    public required string ErrorCode { get; init; }
    
    /// <summary>
    /// Trace ID from the current activity/span.
    /// </summary>
    [JsonPropertyName("trace_id")]
    public required string TraceId { get; init; }
    
    /// <summary>
    /// Optional correlation ID provided by caller.
    /// </summary>
    [JsonPropertyName("correlation_id")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? CorrelationId { get; init; }
    
    /// <summary>
    /// Validation errors for 400 responses.
    /// </summary>
    [JsonPropertyName("errors")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public ValidationMessageDto[]? Errors { get; init; }
}
