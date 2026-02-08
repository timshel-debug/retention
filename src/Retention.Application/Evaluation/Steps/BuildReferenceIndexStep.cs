using Retention.Application.Indexing;

namespace Retention.Application.Evaluation.Steps;

/// <summary>
/// Step 2: Builds the reference index (lookup dictionaries) from validated inputs.
/// </summary>
public sealed class BuildReferenceIndexStep : IEvaluationStep
{
    private readonly IReferenceIndexBuilder _builder;

    public BuildReferenceIndexStep(IReferenceIndexBuilder builder)
    {
        _builder = builder;
    }

    public void Execute(RetentionEvaluationContext context)
    {
        context.ReferenceIndex = _builder.Build(
            context.Projects,
            context.Environments,
            context.Releases);
    }
}
