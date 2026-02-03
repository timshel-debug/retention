using System.Diagnostics;
using Retention.Api.Contracts;
using Retention.Application;
using Retention.Application.Models;
using Retention.Domain.Entities;
using DomainEnvironment = Retention.Domain.Entities.Environment;

namespace Retention.Api.Services;

/// <summary>
/// Adapter that wraps the domain EvaluateRetentionService to the API port.
/// </summary>
public sealed class RetentionEvaluatorAdapter : IRetentionEvaluator
{
    private readonly EvaluateRetentionService _service;
    private static readonly ActivitySource ActivitySource = new("Retention.Api", "1.0.0");

    public RetentionEvaluatorAdapter(EvaluateRetentionService service)
    {
        _service = service;
    }

    public EvaluateRetentionResponse Evaluate(DatasetDto dataset, int releasesToKeep, string? correlationId)
    {
        using var activity = ActivitySource.StartActivity("api.retention.evaluate", ActivityKind.Internal);
        
        activity?.SetTag("projects_count", dataset.Projects.Length);
        activity?.SetTag("environments_count", dataset.Environments.Length);
        activity?.SetTag("releases_count", dataset.Releases.Length);
        activity?.SetTag("deployments_count", dataset.Deployments.Length);
        activity?.SetTag("releases_to_keep", releasesToKeep);
        
        // Convert DTOs to domain entities (normalized by stable sort keys)
        var projects = NormalizeAndConvertProjects(dataset.Projects);
        var environments = NormalizeAndConvertEnvironments(dataset.Environments);
        var releases = NormalizeAndConvertReleases(dataset.Releases);
        var deployments = NormalizeAndConvertDeployments(dataset.Deployments);
        
        // Call domain service
        var result = _service.EvaluateRetention(
            projects, environments, releases, deployments, releasesToKeep, correlationId);
        
        activity?.SetTag("kept_releases_count", result.KeptReleases.Count);
        activity?.SetTag("invalid_deployments_excluded", result.Diagnostics.InvalidDeploymentsExcluded);
        
        // Convert to response DTOs with stable ordering
        return MapToResponse(result, correlationId);
    }

    private static IReadOnlyList<Project> NormalizeAndConvertProjects(ProjectDto[] dtos)
    {
        return dtos
            .OrderBy(p => p.Id, StringComparer.Ordinal)
            .Select(p => new Project(p.Id, p.Name))
            .ToList();
    }

    private static IReadOnlyList<DomainEnvironment> NormalizeAndConvertEnvironments(EnvironmentDto[] dtos)
    {
        return dtos
            .OrderBy(e => e.Id, StringComparer.Ordinal)
            .Select(e => new DomainEnvironment(e.Id, e.Name))
            .ToList();
    }

    private static IReadOnlyList<Release> NormalizeAndConvertReleases(ReleaseDto[] dtos)
    {
        return dtos
            .OrderBy(r => r.Id, StringComparer.Ordinal)
            .Select(r => new Release(r.Id, r.ProjectId, r.Version, r.Created))
            .ToList();
    }

    private static IReadOnlyList<Deployment> NormalizeAndConvertDeployments(DeploymentDto[] dtos)
    {
        return dtos
            .OrderBy(d => d.Id, StringComparer.Ordinal)
            .Select(d => new Deployment(d.Id, d.ReleaseId, d.EnvironmentId, d.DeployedAt))
            .ToList();
    }

    private static EvaluateRetentionResponse MapToResponse(RetentionResult result, string? correlationId)
    {
        // Map kept releases with stable ordering (projectId, environmentId, rank, releaseId)
        var keptReleases = result.KeptReleases
            .OrderBy(k => k.ProjectId, StringComparer.Ordinal)
            .ThenBy(k => k.EnvironmentId, StringComparer.Ordinal)
            .ThenBy(k => k.Rank)
            .ThenBy(k => k.ReleaseId, StringComparer.Ordinal)
            .Select(k => new KeptReleaseDto
            {
                ReleaseId = k.ReleaseId,
                ProjectId = k.ProjectId,
                EnvironmentId = k.EnvironmentId,
                Version = k.Version,
                Created = k.Created,
                LatestDeployedAt = k.LatestDeployedAt,
                Rank = k.Rank,
                ReasonCode = k.ReasonCode
            })
            .ToArray();

        // Map decisions with stable ordering (kindSort, projectId, environmentId, rank, reasonCode, releaseId)
        var decisions = result.Decisions
            .OrderBy(d => d.ReasonCode.StartsWith("kept.", StringComparison.Ordinal) ? 0 : 1)
            .ThenBy(d => d.ProjectId, StringComparer.Ordinal)
            .ThenBy(d => d.EnvironmentId, StringComparer.Ordinal)
            .ThenBy(d => d.Rank)
            .ThenBy(d => d.ReasonCode, StringComparer.Ordinal)
            .ThenBy(d => d.ReleaseId, StringComparer.Ordinal)
            .Select(d => new DecisionDto
            {
                ProjectId = d.ProjectId,
                EnvironmentId = d.EnvironmentId,
                ReleaseId = d.ReleaseId,
                N = d.N,
                Rank = d.Rank,
                LatestDeployedAt = d.LatestDeployedAt,
                ReasonCode = d.ReasonCode,
                ReasonText = d.ReasonText
            })
            .ToArray();

        return new EvaluateRetentionResponse
        {
            KeptReleases = keptReleases,
            Decisions = decisions,
            Diagnostics = new DiagnosticsDto
            {
                GroupsEvaluated = result.Diagnostics.GroupsEvaluated,
                InvalidDeploymentsExcluded = result.Diagnostics.InvalidDeploymentsExcluded,
                TotalKeptReleases = result.Diagnostics.TotalKeptReleases
            },
            CorrelationId = correlationId
        };
    }
}
