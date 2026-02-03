using System.Diagnostics;
using Retention.Api.Contracts;
using Retention.Api.Errors;

namespace Retention.Api.Services;

/// <summary>
/// Validates datasets for structural and referential integrity.
/// Returns deterministic, stable-ordered validation messages.
/// </summary>
public sealed class DatasetValidatorService : IDatasetValidator
{
    private static readonly ActivitySource ActivitySource = new("Retention.Api", "1.0.0");

    public ValidateDatasetResponse Validate(DatasetDto dataset, string? correlationId)
    {
        using var activity = ActivitySource.StartActivity("api.dataset.validate", ActivityKind.Internal);
        
        activity?.SetTag("projects_count", dataset.Projects.Length);
        activity?.SetTag("environments_count", dataset.Environments.Length);
        activity?.SetTag("releases_count", dataset.Releases.Length);
        activity?.SetTag("deployments_count", dataset.Deployments.Length);
        
        var errors = new List<ValidationMessageDto>();
        var warnings = new List<ValidationMessageDto>();
        
        // Validate projects
        ValidateUniqueIds(dataset.Projects, p => p.Id, "projects", ApiErrorCodes.DuplicateProjectId, errors);
        ValidateRequiredFields(dataset.Projects, "projects", errors);
        
        // Validate environments
        ValidateUniqueIds(dataset.Environments, e => e.Id, "environments", ApiErrorCodes.DuplicateEnvironmentId, errors);
        ValidateRequiredFields(dataset.Environments, "environments", errors);
        
        // Validate releases
        ValidateUniqueIds(dataset.Releases, r => r.Id, "releases", ApiErrorCodes.DuplicateReleaseId, errors);
        ValidateReleaseReferences(dataset, errors, warnings);
        
        // Validate deployments
        ValidateUniqueIds(dataset.Deployments, d => d.Id, "deployments", ApiErrorCodes.DuplicateDeploymentId, errors);
        ValidateDeploymentReferences(dataset, errors, warnings);
        
        // Sort errors and warnings for determinism: (code, path, message)
        var sortedErrors = errors
            .OrderBy(e => e.Code, StringComparer.Ordinal)
            .ThenBy(e => e.Path ?? "", StringComparer.Ordinal)
            .ThenBy(e => e.Message, StringComparer.Ordinal)
            .ToArray();
        
        var sortedWarnings = warnings
            .OrderBy(w => w.Code, StringComparer.Ordinal)
            .ThenBy(w => w.Path ?? "", StringComparer.Ordinal)
            .ThenBy(w => w.Message, StringComparer.Ordinal)
            .ToArray();
        
        activity?.SetTag("error_count", sortedErrors.Length);
        activity?.SetTag("warning_count", sortedWarnings.Length);
        
        return new ValidateDatasetResponse
        {
            IsValid = sortedErrors.Length == 0,
            Errors = sortedErrors,
            Warnings = sortedWarnings,
            Summary = new ValidationSummaryDto
            {
                ProjectCount = dataset.Projects.Length,
                EnvironmentCount = dataset.Environments.Length,
                ReleaseCount = dataset.Releases.Length,
                DeploymentCount = dataset.Deployments.Length,
                ErrorCount = sortedErrors.Length,
                WarningCount = sortedWarnings.Length
            }
        };
    }

    private static void ValidateUniqueIds<T>(
        T[] items, 
        Func<T, string> idSelector, 
        string collectionName,
        string errorCode,
        List<ValidationMessageDto> errors)
    {
        var seenIds = new HashSet<string>(StringComparer.Ordinal);
        var duplicates = new HashSet<string>(StringComparer.Ordinal);
        
        for (int i = 0; i < items.Length; i++)
        {
            var id = idSelector(items[i]);
            if (!seenIds.Add(id))
            {
                duplicates.Add(id);
            }
        }
        
        foreach (var duplicateId in duplicates.OrderBy(d => d, StringComparer.Ordinal))
        {
            errors.Add(new ValidationMessageDto
            {
                Code = errorCode,
                Message = $"Duplicate ID '{duplicateId}' found in {collectionName}.",
                Path = $"{collectionName}[].id"
            });
        }
    }

    private static void ValidateRequiredFields(ProjectDto[] projects, string collectionName, List<ValidationMessageDto> errors)
    {
        for (int i = 0; i < projects.Length; i++)
        {
            if (string.IsNullOrWhiteSpace(projects[i].Id))
            {
                errors.Add(new ValidationMessageDto
                {
                    Code = ApiErrorCodes.MissingRequiredField,
                    Message = "Project ID is required.",
                    Path = $"{collectionName}[{i}].id"
                });
            }
            if (string.IsNullOrWhiteSpace(projects[i].Name))
            {
                errors.Add(new ValidationMessageDto
                {
                    Code = ApiErrorCodes.MissingRequiredField,
                    Message = "Project name is required.",
                    Path = $"{collectionName}[{i}].name"
                });
            }
        }
    }

    private static void ValidateRequiredFields(EnvironmentDto[] environments, string collectionName, List<ValidationMessageDto> errors)
    {
        for (int i = 0; i < environments.Length; i++)
        {
            if (string.IsNullOrWhiteSpace(environments[i].Id))
            {
                errors.Add(new ValidationMessageDto
                {
                    Code = ApiErrorCodes.MissingRequiredField,
                    Message = "Environment ID is required.",
                    Path = $"{collectionName}[{i}].id"
                });
            }
            if (string.IsNullOrWhiteSpace(environments[i].Name))
            {
                errors.Add(new ValidationMessageDto
                {
                    Code = ApiErrorCodes.MissingRequiredField,
                    Message = "Environment name is required.",
                    Path = $"{collectionName}[{i}].name"
                });
            }
        }
    }

    private static void ValidateReleaseReferences(DatasetDto dataset, List<ValidationMessageDto> errors, List<ValidationMessageDto> warnings)
    {
        var projectIds = new HashSet<string>(dataset.Projects.Select(p => p.Id), StringComparer.Ordinal);
        
        for (int i = 0; i < dataset.Releases.Length; i++)
        {
            var release = dataset.Releases[i];
            
            if (string.IsNullOrWhiteSpace(release.Id))
            {
                errors.Add(new ValidationMessageDto
                {
                    Code = ApiErrorCodes.MissingRequiredField,
                    Message = "Release ID is required.",
                    Path = $"releases[{i}].id"
                });
            }
            
            if (string.IsNullOrWhiteSpace(release.ProjectId))
            {
                errors.Add(new ValidationMessageDto
                {
                    Code = ApiErrorCodes.MissingRequiredField,
                    Message = "Release projectId is required.",
                    Path = $"releases[{i}].projectId"
                });
            }
            else if (!projectIds.Contains(release.ProjectId))
            {
                warnings.Add(new ValidationMessageDto
                {
                    Code = ApiErrorCodes.InvalidReference,
                    Message = $"Release '{release.Id}' references unknown project '{release.ProjectId}'.",
                    Path = $"releases[{i}].projectId"
                });
            }
        }
    }

    private static void ValidateDeploymentReferences(DatasetDto dataset, List<ValidationMessageDto> errors, List<ValidationMessageDto> warnings)
    {
        var releaseIds = new HashSet<string>(dataset.Releases.Select(r => r.Id), StringComparer.Ordinal);
        var environmentIds = new HashSet<string>(dataset.Environments.Select(e => e.Id), StringComparer.Ordinal);
        
        for (int i = 0; i < dataset.Deployments.Length; i++)
        {
            var deployment = dataset.Deployments[i];
            
            if (string.IsNullOrWhiteSpace(deployment.Id))
            {
                errors.Add(new ValidationMessageDto
                {
                    Code = ApiErrorCodes.MissingRequiredField,
                    Message = "Deployment ID is required.",
                    Path = $"deployments[{i}].id"
                });
            }
            
            if (string.IsNullOrWhiteSpace(deployment.ReleaseId))
            {
                errors.Add(new ValidationMessageDto
                {
                    Code = ApiErrorCodes.MissingRequiredField,
                    Message = "Deployment releaseId is required.",
                    Path = $"deployments[{i}].releaseId"
                });
            }
            else if (!releaseIds.Contains(deployment.ReleaseId))
            {
                warnings.Add(new ValidationMessageDto
                {
                    Code = ApiErrorCodes.InvalidReference,
                    Message = $"Deployment '{deployment.Id}' references unknown release '{deployment.ReleaseId}'.",
                    Path = $"deployments[{i}].releaseId"
                });
            }
            
            if (string.IsNullOrWhiteSpace(deployment.EnvironmentId))
            {
                errors.Add(new ValidationMessageDto
                {
                    Code = ApiErrorCodes.MissingRequiredField,
                    Message = "Deployment environmentId is required.",
                    Path = $"deployments[{i}].environmentId"
                });
            }
            else if (!environmentIds.Contains(deployment.EnvironmentId))
            {
                warnings.Add(new ValidationMessageDto
                {
                    Code = ApiErrorCodes.InvalidReference,
                    Message = $"Deployment '{deployment.Id}' references unknown environment '{deployment.EnvironmentId}'.",
                    Path = $"deployments[{i}].environmentId"
                });
            }
        }
    }
}
