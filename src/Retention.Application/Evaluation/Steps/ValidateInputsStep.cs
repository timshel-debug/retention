using Retention.Application.Validation;

namespace Retention.Application.Evaluation.Steps;

/// <summary>
/// Step 1: Validates inputs using the validation rule chain.
/// Throws <see cref="Errors.ValidationException"/> on failure.
/// </summary>
public sealed class ValidateInputsStep : IEvaluationStep
{
    private readonly IReadOnlyList<IValidationRule> _rules;

    public ValidateInputsStep(IReadOnlyList<IValidationRule> rules)
    {
        _rules = rules;
    }

    public void Execute(RetentionEvaluationContext context)
    {
        var validationContext = new ValidationContext(
            context.Projects,
            context.Environments,
            context.Releases,
            context.Deployments,
            context.ReleasesToKeep);

        foreach (var rule in _rules)
        {
            rule.Validate(validationContext);
        }
    }
}
