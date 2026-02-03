using Microsoft.AspNetCore.Mvc;
using Retention.Api.Contracts;
using Retention.Api.Services;

namespace Retention.Api.Controllers;

/// <summary>
/// Controller for dataset validation operations.
/// </summary>
[ApiController]
[Route("api/v1/datasets")]
[Produces("application/json")]
[RequestSizeLimit(10_000_000)] // 10 MB limit
public sealed class DatasetsController : ControllerBase
{
    private readonly IDatasetValidator _validator;
    private readonly ILogger<DatasetsController> _logger;

    public DatasetsController(IDatasetValidator validator, ILogger<DatasetsController> logger)
    {
        _validator = validator;
        _logger = logger;
    }

    /// <summary>
    /// Validates a dataset for structural and referential integrity.
    /// </summary>
    /// <param name="request">The validation request containing the dataset.</param>
    /// <returns>The validation result with errors, warnings, and summary.</returns>
    /// <response code="200">Validation completed (check isValid for result).</response>
    /// <response code="400">Invalid request payload.</response>
    /// <response code="429">Rate limit exceeded.</response>
    /// <response code="500">Internal server error.</response>
    [HttpPost("validate")]
    [ProducesResponseType(typeof(ValidateDatasetResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiProblemDetails), StatusCodes.Status429TooManyRequests)]
    [ProducesResponseType(typeof(ApiProblemDetails), StatusCodes.Status500InternalServerError)]
    public IActionResult Validate([FromBody] ValidateDatasetRequest request)
    {
        _logger.LogInformation(
            "Validating dataset: Projects={ProjectCount}, Environments={EnvCount}, Releases={ReleaseCount}, Deployments={DeploymentCount}, CorrelationId={CorrelationId}",
            request.Dataset.Projects.Length,
            request.Dataset.Environments.Length,
            request.Dataset.Releases.Length,
            request.Dataset.Deployments.Length,
            request.CorrelationId);
        
        var result = _validator.Validate(request.Dataset, request.CorrelationId);
        
        return Ok(result);
    }
}
