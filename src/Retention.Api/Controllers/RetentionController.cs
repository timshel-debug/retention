using Microsoft.AspNetCore.Mvc;
using Retention.Api.Contracts;
using Retention.Api.Errors;
using Retention.Api.Services;
using Retention.Application.Errors;

namespace Retention.Api.Controllers;

/// <summary>
/// Controller for retention evaluation operations.
/// </summary>
[ApiController]
[Route("api/v1/retention")]
[Produces("application/json")]
[RequestSizeLimit(10_000_000)] // 10 MB limit
public sealed class RetentionController : ControllerBase
{
    private readonly IRetentionEvaluator _evaluator;
    private readonly ILogger<RetentionController> _logger;

    public RetentionController(IRetentionEvaluator evaluator, ILogger<RetentionController> logger)
    {
        _evaluator = evaluator;
        _logger = logger;
    }

    /// <summary>
    /// Evaluates retention policy for the given dataset.
    /// </summary>
    /// <param name="request">The evaluation request containing dataset and releasesToKeep.</param>
    /// <returns>The evaluation result with kept releases, decisions, and diagnostics.</returns>
    /// <response code="200">Evaluation completed successfully.</response>
    /// <response code="400">Invalid request (validation error).</response>
    /// <response code="429">Rate limit exceeded.</response>
    /// <response code="500">Internal server error.</response>
    [HttpPost("evaluate")]
    [ProducesResponseType(typeof(EvaluateRetentionResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiProblemDetails), StatusCodes.Status429TooManyRequests)]
    [ProducesResponseType(typeof(ApiProblemDetails), StatusCodes.Status500InternalServerError)]
    public IActionResult Evaluate([FromBody] EvaluateRetentionRequest request)
    {
        // Boundary validation: releasesToKeep >= 0
        if (request.ReleasesToKeep < 0)
        {
            throw new ValidationException(
                ApiErrorCodes.NNegative,
                $"Parameter 'releasesToKeep' must be >= 0, but was {request.ReleasesToKeep}.");
        }
        
        _logger.LogInformation(
            "Evaluating retention: ReleasesToKeep={ReleasesToKeep}, Projects={ProjectCount}, Environments={EnvCount}, Releases={ReleaseCount}, Deployments={DeploymentCount}, CorrelationId={CorrelationId}",
            request.ReleasesToKeep,
            request.Dataset.Projects.Length,
            request.Dataset.Environments.Length,
            request.Dataset.Releases.Length,
            request.Dataset.Deployments.Length,
            request.CorrelationId);
        
        var result = _evaluator.Evaluate(request.Dataset, request.ReleasesToKeep, request.CorrelationId);
        
        return Ok(result);
    }
}
