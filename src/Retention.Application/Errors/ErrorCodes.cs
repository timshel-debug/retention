namespace Retention.Application.Errors;

/// <summary>
/// Standard error codes for validation and domain errors.
/// </summary>
public static class ErrorCodes
{
    // Validation errors
    public const string NNegative = "validation.n_negative";
    public const string NullElement = "validation.null_element";
    public const string DuplicateProjectId = "validation.duplicate_id.project";
    public const string DuplicateEnvironmentId = "validation.duplicate_id.environment";
    public const string DuplicateReleaseId = "validation.duplicate_id.release";
    
    // Domain errors
    public const string DomainInvariant = "domain.invariant_violation";
}
