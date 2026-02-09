using Retention.Application.Errors;
using Retention.Application.Validation.Rules;
using Retention.Domain.Entities;
using Environment = Retention.Domain.Entities.Environment;

namespace Retention.Application.Validation;

/// <summary>
/// Creates the default validation rule chain in the correct execution order.
/// </summary>
public static class ValidationRuleChainFactory
{
    /// <summary>
    /// Returns the ordered list of validation rules matching the original validation sequence.
    /// </summary>
    public static IReadOnlyList<IValidationRule> CreateDefaultChain()
    {
        return
        [
	        // 1. Non-negative releasesToKeep (checked before null coalescing in original)
            new NonNegativeReleasesToKeepRule(),
            
            // 2. No null elements (same order as original)
            new NoNullElementsRule<Project>(ctx => ctx.Projects, "projects"),
            new NoNullElementsRule<Environment>(ctx => ctx.Environments, "environments"),
            new NoNullElementsRule<Release>(ctx => ctx.Releases, "releases"),
            new NoNullElementsRule<Deployment>(ctx => ctx.Deployments, "deployments"),

            // 3. No duplicate IDs (same order as original)
            new NoDuplicateIdsRule<Project>(
                ctx => ctx.Projects, p => p.Id, "project", ErrorCodes.DuplicateProjectId),
            new NoDuplicateIdsRule<Environment>(
                ctx => ctx.Environments, e => e.Id, "environment", ErrorCodes.DuplicateEnvironmentId),
            new NoDuplicateIdsRule<Release>(
                ctx => ctx.Releases, r => r.Id, "release", ErrorCodes.DuplicateReleaseId)
        ];
    }
}
