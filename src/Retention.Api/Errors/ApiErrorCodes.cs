namespace Retention.Api.Errors;

/// <summary>
/// Stable error codes for API responses.
/// </summary>
public static class ApiErrorCodes
{
    // Validation errors (400)
    public const string NNegative = "validation.n_negative";
    public const string NullElement = "validation.null_element";
    public const string MissingRequiredField = "validation.missing_required_field";
    public const string DuplicateProjectId = "validation.duplicate_project_id";
    public const string DuplicateEnvironmentId = "validation.duplicate_environment_id";
    public const string DuplicateReleaseId = "validation.duplicate_release_id";
    public const string DuplicateDeploymentId = "validation.duplicate_deployment_id";
    public const string InvalidReference = "validation.invalid_reference";
    public const string InvalidPayload = "validation.invalid_payload";
    public const string PayloadTooLarge = "validation.payload_too_large";
    
    // Auth errors (401/403)
    public const string Unauthorized = "auth.unauthorized";
    public const string Forbidden = "auth.forbidden";
    
    // Rate limiting (429)
    public const string RateLimited = "rate_limited";
    
    // Server errors (500)
    public const string InternalError = "internal_error";
    public const string DomainInvariant = "domain_invariant";
}
