using System.Diagnostics;
using System.Text.Json;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.RateLimiting;
using Retention.Api.Contracts;
using Retention.Api.Errors;

namespace Retention.Api.RateLimiting;

/// <summary>
/// Configuration and setup for rate limiting.
/// </summary>
public static class RateLimitingConfiguration
{
    public const string PolicyName = "api";
    
    /// <summary>
    /// Configures rate limiting services.
    /// </summary>
    public static IServiceCollection AddApiRateLimiting(this IServiceCollection services, IConfiguration configuration)
    {
        var enabled = configuration.GetValue<bool>("RateLimiting:Enabled");
        
        if (!enabled)
        {
            return services;
        }
        
        var permitLimit = configuration.GetValue<int>("RateLimiting:PermitLimit", 100);
        var windowSeconds = configuration.GetValue<int>("RateLimiting:WindowSeconds", 60);
        
        services.AddRateLimiter(options =>
        {
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
            
            options.OnRejected = async (context, cancellationToken) =>
            {
                var traceId = Activity.Current?.TraceId.ToString() ?? context.HttpContext.TraceIdentifier;
                var correlationId = context.HttpContext.Request.Headers["X-Correlation-ID"].FirstOrDefault();
                
                var problemDetails = new ApiProblemDetails
                {
                    Type = "https://httpstatuses.com/429",
                    Title = "Too Many Requests",
                    Status = StatusCodes.Status429TooManyRequests,
                    Detail = "Rate limit exceeded. Please try again later.",
                    Instance = context.HttpContext.Request.Path,
                    ErrorCode = ApiErrorCodes.RateLimited,
                    TraceId = traceId,
                    CorrelationId = correlationId
                };
                
                context.HttpContext.Response.StatusCode = StatusCodes.Status429TooManyRequests;
                context.HttpContext.Response.ContentType = "application/problem+json";
                
                await context.HttpContext.Response.WriteAsJsonAsync(problemDetails, new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                    DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
                }, cancellationToken);
            };
            
            options.AddFixedWindowLimiter(PolicyName, windowOptions =>
            {
                windowOptions.PermitLimit = permitLimit;
                windowOptions.Window = TimeSpan.FromSeconds(windowSeconds);
                windowOptions.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
                windowOptions.QueueLimit = 0;
            });
        });
        
        return services;
    }
    
    /// <summary>
    /// Adds rate limiting middleware if enabled.
    /// </summary>
    public static IApplicationBuilder UseApiRateLimiting(this IApplicationBuilder app, IConfiguration configuration)
    {
        var enabled = configuration.GetValue<bool>("RateLimiting:Enabled");
        
        if (enabled)
        {
            app.UseRateLimiter();
        }
        
        return app;
    }
}
