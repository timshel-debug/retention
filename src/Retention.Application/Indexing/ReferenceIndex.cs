using Retention.Domain.Entities;
using Environment = Retention.Domain.Entities.Environment;

namespace Retention.Application.Indexing;

/// <summary>
/// Immutable lookup index for projects, environments, and releases by ID.
/// Built after validation ensures no duplicates.
/// </summary>
public sealed class ReferenceIndex
{
    public IReadOnlyDictionary<string, Project> ProjectsById { get; }
    public IReadOnlyDictionary<string, Environment> EnvironmentsById { get; }
    public IReadOnlyDictionary<string, Release> ReleasesById { get; }

    public ReferenceIndex(
        IReadOnlyDictionary<string, Project> projectsById,
        IReadOnlyDictionary<string, Environment> environmentsById,
        IReadOnlyDictionary<string, Release> releasesById)
    {
        ProjectsById = projectsById ?? throw new ArgumentNullException(nameof(projectsById));
        EnvironmentsById = environmentsById ?? throw new ArgumentNullException(nameof(environmentsById));
        ReleasesById = releasesById ?? throw new ArgumentNullException(nameof(releasesById));
    }
}
