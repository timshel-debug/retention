using Retention.Api.Contracts;

namespace Retention.Api.Services;

/// <summary>
/// Port for the dataset validation service.
/// </summary>
public interface IDatasetValidator
{
    /// <summary>
    /// Validates a dataset for structural and referential integrity.
    /// </summary>
    ValidateDatasetResponse Validate(DatasetDto dataset, string? correlationId);
}
