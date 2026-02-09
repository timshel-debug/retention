using System.Reflection;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Retention.Application;
using Retention.Application.DependencyInjection;
using Retention.Application.Evaluation;
using Retention.Application.Evaluation.Steps;
using Retention.Application.Indexing;
using Retention.Application.Mapping;
using Retention.Application.Specifications;
using Retention.Application.Validation;
using Retention.Domain.Services;

namespace Retention.UnitTests.Application;

/// <summary>
/// Architecture guardrail tests ensuring DI compliance:
/// - No service types with public parameterless constructors (forces DI usage)
/// - DI container correctly resolves all key services
/// </summary>
public class ArchitectureGuardrailTests
{
    /// <summary>
    /// Service types that must NOT have public parameterless constructors.
    /// These types should only be created via DI.
    /// Exempt: value objects, records, DTOs, static classes, strategies, and simple helpers.
    /// </summary>
    private static readonly Type[] ServiceTypes =
    [
        typeof(RetentionPolicyEvaluator),
        typeof(DefaultGroupRetentionEvaluator),
        typeof(EvaluateRetentionService),
        typeof(RetentionEvaluationEngine),
    ];

    [Theory]
    [MemberData(nameof(ServiceTypeData))]
    public void ServiceType_ShouldNotHavePublicParameterlessConstructor(Type serviceType)
    {
        var parameterlessCtor = serviceType.GetConstructor(
            BindingFlags.Public | BindingFlags.Instance, Type.EmptyTypes);

        parameterlessCtor.Should().BeNull(
            $"{serviceType.Name} should not have a public parameterless constructor — " +
            "service graph wiring must happen in the composition root");
    }

    public static TheoryData<Type> ServiceTypeData()
    {
        var data = new TheoryData<Type>();
        foreach (var type in ServiceTypes)
            data.Add(type);
        return data;
    }

    [Fact]
    public void DiContainer_ResolvesEvaluateRetentionService()
    {
        var services = new ServiceCollection();
        services.AddRetentionApplication();
        var provider = services.BuildServiceProvider();

        var service = provider.GetService<IEvaluateRetentionService>();

        service.Should().NotBeNull("IEvaluateRetentionService should be resolvable from DI");
        service.Should().BeOfType<EvaluateRetentionService>();
    }

    [Fact]
    public void DiContainer_ResolvesRetentionEvaluationEngine()
    {
        var services = new ServiceCollection();
        services.AddRetentionApplication();
        var provider = services.BuildServiceProvider();

        var engine = provider.GetService<RetentionEvaluationEngine>();

        engine.Should().NotBeNull("RetentionEvaluationEngine should be resolvable from DI");
    }

    [Fact]
    public void DiContainer_ResolvesAllPipelineHelpers()
    {
        var services = new ServiceCollection();
        services.AddRetentionApplication();
        var provider = services.BuildServiceProvider();

        provider.GetService<IRetentionRankingStrategy>().Should().NotBeNull();
        provider.GetService<IRetentionSelectionStrategy>().Should().NotBeNull();
        provider.GetService<IGroupRetentionEvaluator>().Should().NotBeNull();
        provider.GetService<IRetentionPolicyEvaluator>().Should().NotBeNull();
        provider.GetService<IReferenceIndexBuilder>().Should().NotBeNull();
        provider.GetService<IDeploymentValiditySpecification>().Should().NotBeNull();
        provider.GetService<IDecisionLogAssembler>().Should().NotBeNull();
        provider.GetService<IKeptReleaseMapper>().Should().NotBeNull();
        provider.GetService<IDiagnosticsCalculator>().Should().NotBeNull();
        provider.GetService<IReadOnlyList<IValidationRule>>().Should().NotBeNull();
        provider.GetService<IReadOnlyList<IEvaluationStep>>().Should().NotBeNull();
    }

    [Fact]
    public void DiContainer_EvaluateRetentionService_CanEvaluate()
    {
        // Smoke test: resolve from DI and execute a simple evaluation
        var services = new ServiceCollection();
        services.AddRetentionApplication();
        var provider = services.BuildServiceProvider();
        var service = provider.GetRequiredService<IEvaluateRetentionService>();

        var result = service.EvaluateRetention(null, null, null, null, releasesToKeep: 1);

        result.Should().NotBeNull();
        result.KeptReleases.Should().BeEmpty();
    }
}
