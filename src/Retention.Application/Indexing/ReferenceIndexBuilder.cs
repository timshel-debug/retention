using Retention.Domain.Entities;
using Environment = Retention.Domain.Entities.Environment;

namespace Retention.Application.Indexing;

/// <summary>
/// Builds a <see cref="ReferenceIndex"/> from validated entity lists.
/// Assumes duplicates have already been validated; does not re-check.
/// </summary>
public interface IReferenceIndexBuilder
{
    ReferenceIndex Build(
        IReadOnlyList<Project> projects,
        IReadOnlyList<Environment> environments,
        IReadOnlyList<Release> releases);
}

/// <summary>
/// Default builder using ordinal key comparisons (matching prior ToDictionary behavior).
/// </summary>
public sealed class ReferenceIndexBuilder : IReferenceIndexBuilder
{
    public ReferenceIndex Build(
        IReadOnlyList<Project> projects,
        IReadOnlyList<Environment> environments,
        IReadOnlyList<Release> releases)
    {
        ArgumentNullException.ThrowIfNull(projects);
        ArgumentNullException.ThrowIfNull(environments);
        ArgumentNullException.ThrowIfNull(releases);

        var projectsById = projects.ToDictionary(p => p.Id, StringComparer.Ordinal);
        var environmentsById = environments.ToDictionary(e => e.Id, StringComparer.Ordinal);
        var releasesById = releases.ToDictionary(r => r.Id, StringComparer.Ordinal);

        return new ReferenceIndex(projectsById, environmentsById, releasesById);
    }
}
