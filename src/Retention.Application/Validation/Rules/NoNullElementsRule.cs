using Retention.Application.Errors;

namespace Retention.Application.Validation.Rules;

/// <summary>
/// Validates that no elements in a list are null.
/// Maps to <see cref="ErrorCodes.NullElement"/>.
/// </summary>
public sealed class NoNullElementsRule<T> : IValidationRule where T : class
{
    private readonly Func<ValidationContext, IReadOnlyList<T>> _accessor;
    private readonly string _paramName;

    public NoNullElementsRule(Func<ValidationContext, IReadOnlyList<T>> accessor, string paramName)
    {
        _accessor = accessor;
        _paramName = paramName;
    }

    public void Validate(ValidationContext context)
    {
        var collection = _accessor(context);
        for (int i = 0; i < collection.Count; i++)
        {
            if (collection[i] is null)
            {
                throw new ValidationException(
                    ErrorCodes.NullElement,
                    $"Null element found at index {i} in '{_paramName}'.");
            }
        }
    }
}
