namespace Retention.Application.Errors;

/// <summary>
/// Exception thrown when input validation fails.
/// </summary>
public class ValidationException : Exception
{
    /// <summary>
    /// Stable error code for programmatic handling.
    /// </summary>
    public string Code { get; }

    public ValidationException(string code, string message) : base(message)
    {
        Code = code;
    }
}
