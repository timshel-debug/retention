using System.Diagnostics;
using System.Text.Json;
using Retention.Api.Contracts;
using Retention.Api.Errors;
using Retention.Application.Errors;

namespace Retention.Api.Middleware;

/// <summary>
/// Global exception middleware that converts exceptions to RFC7807 ProblemDetails.
/// Never exposes stack traces or sensitive internals.
/// </summary>
public sealed class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;

    public ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            await HandleExceptionAsync(context, ex);
        }
    }

    private async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        var traceId = Activity.Current?.TraceId.ToString() ?? context.TraceIdentifier;
        var correlationId = context.Request.Headers["X-Correlation-ID"].FirstOrDefault();
        
        var (statusCode, errorCode, title, detail) = MapException(exception);
        
        // Log once at boundary with structured fields
        _logger.LogError(
            exception,
            "Request failed: {ErrorCode} {Title}. TraceId={TraceId}, CorrelationId={CorrelationId}, Path={Path}",
            errorCode,
            title,
            traceId,
            correlationId,
            context.Request.Path);
        
        var problemDetails = new ApiProblemDetails
        {
            Type = $"https://httpstatuses.com/{statusCode}",
            Title = title,
            Status = statusCode,
            Detail = detail,
            Instance = context.Request.Path,
            ErrorCode = errorCode,
            TraceId = traceId,
            CorrelationId = correlationId,
            Errors = exception is ValidationException validationEx 
                ? MapValidationErrors(validationEx) 
                : null
        };
        
        context.Response.StatusCode = statusCode;
        context.Response.ContentType = "application/problem+json";
        
        await context.Response.WriteAsJsonAsync(problemDetails, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
        });
    }

    private static (int StatusCode, string ErrorCode, string Title, string? Detail) MapException(Exception exception)
    {
        return exception switch
        {
            ValidationException validationEx => (
                StatusCodes.Status400BadRequest,
                validationEx.Code,
                "Validation Error",
                validationEx.Message),
            
            DomainException domainEx => (
                StatusCodes.Status500InternalServerError,
                ApiErrorCodes.DomainInvariant,
                "Domain Invariant Violation",
                "An internal rule violation occurred."),
            
            JsonException => (
                StatusCodes.Status400BadRequest,
                ApiErrorCodes.InvalidPayload,
                "Invalid Payload",
                "The request body could not be parsed as valid JSON."),
            
            BadHttpRequestException => (
                StatusCodes.Status400BadRequest,
                ApiErrorCodes.InvalidPayload,
                "Invalid Request",
                "The request is malformed."),
            
            _ => (
                StatusCodes.Status500InternalServerError,
                ApiErrorCodes.InternalError,
                "Internal Server Error",
                "An unexpected error occurred.")
        };
    }

    private static ValidationMessageDto[]? MapValidationErrors(ValidationException exception)
    {
        // Return a single validation message based on the exception
        return new[]
        {
            new ValidationMessageDto
            {
                Code = exception.Code,
                Message = exception.Message,
                Path = null
            }
        };
    }
}

/// <summary>
/// Extension methods for registering the exception handling middleware.
/// </summary>
public static class ExceptionHandlingMiddlewareExtensions
{
    public static IApplicationBuilder UseGlobalExceptionHandler(this IApplicationBuilder app)
    {
        return app.UseMiddleware<ExceptionHandlingMiddleware>();
    }
}
