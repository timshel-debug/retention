namespace Retention.Application.Specifications;

/// <summary>
/// Result of evaluating deployment validity against reference index.
/// </summary>
public sealed class DeploymentValidityResult
{
    public bool IsValid { get; }
    public IReadOnlyList<string> Reasons { get; }

    private DeploymentValidityResult(bool isValid, IReadOnlyList<string> reasons)
    {
        IsValid = isValid;
        Reasons = reasons;
    }

    public static DeploymentValidityResult Valid() => new(true, Array.Empty<string>());

    public static DeploymentValidityResult Invalid(IReadOnlyList<string> reasons) => new(false, reasons);
}
