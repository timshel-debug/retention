using Retention.Application.Mapping;

namespace Retention.Application.Evaluation.Steps;

/// <summary>
/// Step 5: Maps domain candidates to KeptRelease DTOs.
/// </summary>
public sealed class MapResultsStep : IEvaluationStep
{
    private readonly IKeptReleaseMapper _mapper;

    public MapResultsStep(IKeptReleaseMapper mapper)
    {
        _mapper = mapper;
    }

    public void Execute(RetentionEvaluationContext context)
    {
        context.KeptReleases = context.DomainCandidates
            .Select(c => _mapper.Map(c))
            .ToList();
    }
}
