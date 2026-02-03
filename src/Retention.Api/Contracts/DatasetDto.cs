using System.Text.Json.Serialization;

namespace Retention.Api.Contracts;

/// <summary>
/// Represents a dataset containing projects, environments, releases, and deployments.
/// </summary>
public sealed record DatasetDto
{
    [JsonPropertyName("projects")]
    public required ProjectDto[] Projects { get; init; }
    
    [JsonPropertyName("environments")]
    public required EnvironmentDto[] Environments { get; init; }
    
    [JsonPropertyName("releases")]
    public required ReleaseDto[] Releases { get; init; }
    
    [JsonPropertyName("deployments")]
    public required DeploymentDto[] Deployments { get; init; }
}

public sealed record ProjectDto
{
    [JsonPropertyName("id")]
    public required string Id { get; init; }
    
    [JsonPropertyName("name")]
    public required string Name { get; init; }
}

public sealed record EnvironmentDto
{
    [JsonPropertyName("id")]
    public required string Id { get; init; }
    
    [JsonPropertyName("name")]
    public required string Name { get; init; }
}

public sealed record ReleaseDto
{
    [JsonPropertyName("id")]
    public required string Id { get; init; }
    
    [JsonPropertyName("projectId")]
    public required string ProjectId { get; init; }
    
    [JsonPropertyName("version")]
    public string? Version { get; init; }
    
    [JsonPropertyName("created")]
    public required DateTimeOffset Created { get; init; }
}

public sealed record DeploymentDto
{
    [JsonPropertyName("id")]
    public required string Id { get; init; }
    
    [JsonPropertyName("releaseId")]
    public required string ReleaseId { get; init; }
    
    [JsonPropertyName("environmentId")]
    public required string EnvironmentId { get; init; }
    
    [JsonPropertyName("deployedAt")]
    public required DateTimeOffset DeployedAt { get; init; }
}
