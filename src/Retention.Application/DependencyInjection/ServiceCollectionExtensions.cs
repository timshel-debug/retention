using Microsoft.Extensions.DependencyInjection;
using Retention.Application.Evaluation;
using Retention.Application.Evaluation.Steps;
using Retention.Application.Indexing;
using Retention.Application.Mapping;
using Retention.Application.Specifications;
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

        // Telemetry decorator wrapping domain evaluator
        services.AddSingleton<IGroupRetentionEvaluator>(sp =>
            new TelemetryGroupRetentionEvaluator(
                new DefaultGroupRetentionEvaluator(
                    sp.GetRequiredService<IRetentionRankingStrategy>(),
                    sp.GetRequiredService<IRetentionSelectionStrategy>())));
        services.AddSingleton<IRetentionPolicyEvaluator>(sp =>
            new RetentionPolicyEvaluator(sp.GetRequiredService<IGroupRetentionEvaluator>()));

        // Pipeline helper services
        services.AddSingleton<IReferenceIndexBuilder, ReferenceIndexBuilder>();
        services.AddSingleton<IDeploymentValiditySpecification, DefaultDeploymentValiditySpecification>();
        services.AddSingleton<IDecisionLogAssembler, DecisionLogAssembler>();
        services.AddSingleton<IKeptReleaseMapper, KeptReleaseMapper>();
        services.AddSingleton<IDiagnosticsCalculator, DiagnosticsCalculator>();

        // Pipeline steps — ordered
        services.AddSingleton<IReadOnlyList<IEvaluationStep>>(sp => new IEvaluationStep[]
        {
            new ValidateInputsStep(sp.GetRequiredService<IReadOnlyList<IValidationRule>>()),
            new BuildReferenceIndexStep(sp.GetRequiredService<IReferenceIndexBuilder>()),
            new FilterInvalidDeploymentsStep(
                sp.GetRequiredService<IDeploymentValiditySpecification>(),
                sp.GetRequiredService<IDecisionLogAssembler>()),
            new EvaluatePolicyStep(sp.GetRequiredService<IRetentionPolicyEvaluator>()),
            new MapResultsStep(sp.GetRequiredService<IKeptReleaseMapper>()),
            new BuildDecisionLogStep(sp.GetRequiredService<IDecisionLogAssembler>()),
            new FinalizeResultStep(sp.GetRequiredService<IDiagnosticsCalculator>()),
        });

        // Engine + application service
        services.AddSingleton<RetentionEvaluationEngine>();
        services.AddSingleton<EvaluateRetentionService>();
        services.AddSingleton<IEvaluateRetentionService>(sp => sp.GetRequiredService<EvaluateRetentionService>());

        return services;
    }
}
