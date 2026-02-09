using Microsoft.Extensions.DependencyInjection;
using Retention.Application.Evaluation;
using Retention.Application.Validation;
using Retention.Domain.DependencyInjection;
using Retention.Domain.Services;

namespace Retention.Application.DependencyInjection;

/// <summary>
/// Registers application + domain services into the DI container.
/// This is the primary composition root extension for the Retention system.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds all Retention application and domain services to the service collection.
    /// </summary>
    public static IServiceCollection AddRetentionApplication(this IServiceCollection services)
    {
        // Register domain services
        services.AddRetentionDomain();

        // Validation rules — ordered chain
        services.AddSingleton<IReadOnlyList<IValidationRule>>(
            _ => ValidationRuleChainFactory.CreateDefaultChain());

        // Application services — matches current Program.cs registrations
        services.AddSingleton<EvaluateRetentionService>();
        services.AddSingleton<IEvaluateRetentionService>(sp => sp.GetRequiredService<EvaluateRetentionService>());

        return services;
    }
}
