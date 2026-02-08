using Retention.Application.Errors;

namespace Retention.Application.Validation.Rules;

/// <summary>
/// Validates that releasesToKeep is non-negative.
/// Maps to <see cref="ErrorCodes.NNegative"/>.
/// </summary>
public sealed class NonNegativeReleasesToKeepRule : IValidationRule
{
    public void Validate(ValidationContext context)
    {
        if (context.ReleasesToKeep < 0)
        {
            throw new ValidationException(
                ErrorCodes.NNegative,
                $"Parameter 'releasesToKeep' must be >= 0, but was {context.ReleasesToKeep}.");
        }
    }
}
