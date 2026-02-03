namespace Retention.Application.Errors;

/// <summary>
/// Exception thrown when a domain invariant is violated.
/// </summary>
public class DomainException : Exception
{
    /// <summary>
    /// Stable error code for programmatic handling.
    /// </summary>
    public string Code { get; }

    public DomainException(string code, string message) : base(message)
    {
        Code = code;
    }
}
