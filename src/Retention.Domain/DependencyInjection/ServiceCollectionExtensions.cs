using Microsoft.Extensions.DependencyInjection;
using Retention.Domain.Services;

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
        services.AddSingleton<IRetentionRankingStrategy, DefaultRankingStrategy>();
        services.AddSingleton<IRetentionSelectionStrategy, TopNSelectionStrategy>();
        return services;
    }
}
