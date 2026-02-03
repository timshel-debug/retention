using Retention.Api.Contracts;

namespace Retention.Api.Services;

/// <summary>
/// Port for the retention evaluator service.
/// </summary>
public interface IRetentionEvaluator
{
    /// <summary>
    /// Evaluates retention policy for the given dataset.
    /// </summary>
    EvaluateRetentionResponse Evaluate(DatasetDto dataset, int releasesToKeep, string? correlationId);
}
