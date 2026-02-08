namespace Retention.Application.Evaluation;

/// <summary>
/// A single step in the retention evaluation pipeline.
/// </summary>
public interface IEvaluationStep
{
    void Execute(RetentionEvaluationContext context);
}
