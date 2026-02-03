using Microsoft.AspNetCore.Mvc;

namespace Retention.Api.Controllers;

/// <summary>
/// Health check endpoints for liveness and readiness probes.
/// </summary>
[ApiController]
[Route("health")]
public sealed class HealthController : ControllerBase
{
    /// <summary>
    /// Liveness probe - returns 200 if the process is running.
    /// </summary>
    /// <returns>OK status indicating the service is alive.</returns>
    /// <response code="200">Service is alive.</response>
    [HttpGet("live")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public IActionResult Live()
    {
        return Ok(new { status = "healthy", timestamp = DateTimeOffset.UtcNow });
    }

    /// <summary>
    /// Readiness probe - returns 200 if the service is ready to accept requests.
    /// </summary>
    /// <returns>OK status indicating the service is ready.</returns>
    /// <response code="200">Service is ready.</response>
    [HttpGet("ready")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public IActionResult Ready()
    {
        // No external dependencies in v1, so always ready if alive
        return Ok(new { status = "ready", timestamp = DateTimeOffset.UtcNow });
    }
}
