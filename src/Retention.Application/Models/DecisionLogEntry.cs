using Retention.Domain.Models;

namespace Retention.Application.Models;

/// <summary>
/// Stable reason codes for decision log entries.
/// </summary>
public static class DecisionReasonCodes
{
    /// <summary>Release is kept because it is in the top N most recently deployed.</summary>
    public const string KeptTopN = ReasonCodes.KeptTopN;
    
    /// <summary>Deployment excluded due to invalid reference to missing entity.</summary>
    public const string InvalidReference = "diagnostic.invalid_reference";
}

/// <summary>
/// Represents a decision entry explaining why a release was kept or why a deployment was excluded.
/// </summary>
public sealed record DecisionLogEntry
{
    public required string ProjectId { get; init; }
    public required string EnvironmentId { get; init; }
    public required string ReleaseId { get; init; }
    public required int N { get; init; }
    public required int Rank { get; init; }
    public required DateTimeOffset? LatestDeployedAt { get; init; }
    public required string ReasonText { get; init; }
    public required string ReasonCode { get; init; }
    public string? CorrelationId { get; init; }
    
    /// <summary>
    /// Decision type for ordering: "kept" entries come before "diagnostic" entries.
    /// Based on explicit reason code mapping rather than string prefix matching.
    /// </summary>
    public string DecisionType => ReasonCode switch
    {
        DecisionReasonCodes.KeptTopN => "kept",
        _ => "diagnostic"
    };
}
