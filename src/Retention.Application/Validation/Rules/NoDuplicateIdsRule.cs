using Retention.Application.Errors;

namespace Retention.Application.Validation.Rules;

/// <summary>
/// Validates that no duplicate IDs exist in a list.
/// Maps to entity-specific duplicate error codes.
/// </summary>
public sealed class NoDuplicateIdsRule<T> : IValidationRule
{
    private readonly Func<ValidationContext, IReadOnlyList<T>> _accessor;
    private readonly Func<T, string> _idSelector;
    private readonly string _entityType;
    private readonly string _errorCode;

    public NoDuplicateIdsRule(
        Func<ValidationContext, IReadOnlyList<T>> accessor,
        Func<T, string> idSelector,
        string entityType,
        string errorCode)
    {
        _accessor = accessor;
        _idSelector = idSelector;
        _entityType = entityType;
        _errorCode = errorCode;
    }

    public void Validate(ValidationContext context)
    {
        var collection = _accessor(context);
        var seenIds = new HashSet<string>(StringComparer.Ordinal);
        var duplicates = new List<string>();

        foreach (var item in collection)
        {
            var id = _idSelector(item);
            if (!seenIds.Add(id))
            {
                if (!duplicates.Contains(id))
                {
                    duplicates.Add(id);
                }
            }
        }

        if (duplicates.Count > 0)
        {
            throw new ValidationException(
                _errorCode,
                $"Duplicate {_entityType} ID(s) found: {string.Join(", ", duplicates)}");
        }
    }
}
