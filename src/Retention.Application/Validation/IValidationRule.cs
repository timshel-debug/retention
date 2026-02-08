namespace Retention.Application.Validation;

/// <summary>
/// A single validation rule in the Chain of Responsibility.
/// </summary>
public interface IValidationRule
{
    /// <summary>
    /// Validates the context. Throws <see cref="Errors.ValidationException"/> on failure.
    /// </summary>
    void Validate(ValidationContext context);
}

/// <summary>
/// Minimal context for validation rules to inspect inputs.
/// </summary>
public sealed class ValidationContext
{
    public IReadOnlyList<Domain.Entities.Project> Projects { get; }
    public IReadOnlyList<Domain.Entities.Environment> Environments { get; }
    public IReadOnlyList<Domain.Entities.Release> Releases { get; }
    public IReadOnlyList<Domain.Entities.Deployment> Deployments { get; }
    public int ReleasesToKeep { get; }

    public ValidationContext(
        IReadOnlyList<Domain.Entities.Project> projects,
        IReadOnlyList<Domain.Entities.Environment> environments,
        IReadOnlyList<Domain.Entities.Release> releases,
        IReadOnlyList<Domain.Entities.Deployment> deployments,
        int releasesToKeep)
    {
        Projects = projects;
        Environments = environments;
        Releases = releases;
        Deployments = deployments;
        ReleasesToKeep = releasesToKeep;
    }
}
