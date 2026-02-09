using Microsoft.Extensions.DependencyInjection;

namespace Retention.Domain.DependencyInjection;

/// <summary>
/// Registers domain services into the DI container.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds Retention domain services to the service collection.
    /// </summary>
    public static IServiceCollection AddRetentionDomain(this IServiceCollection services)
    {
        // Domain services will be registered here as parameterless ctors are removed.
        // For now this is a placeholder that establishes the extension point.
        return services;
    }
}
