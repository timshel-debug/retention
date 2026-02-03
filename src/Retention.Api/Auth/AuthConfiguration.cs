using System.Diagnostics;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Retention.Api.Contracts;
using Retention.Api.Errors;

namespace Retention.Api.Auth;

/// <summary>
/// Configuration and setup for JWT authentication.
/// </summary>
public static class AuthConfiguration
{
    /// <summary>
    /// Configures JWT authentication services if enabled.
    /// </summary>
    public static IServiceCollection AddApiAuthentication(this IServiceCollection services, IConfiguration configuration)
    {
        var enabled = configuration.GetValue<bool>("Auth:Enabled");
        
        if (!enabled)
        {
            return services;
        }
        
        var secretKey = configuration.GetValue<string>("Auth:SecretKey")
            ?? throw new InvalidOperationException(
                "Auth:SecretKey must be configured when Auth:Enabled is true. " +
                "Set a secure secret key of at least 32 characters in configuration or environment variables.");
        var issuer = configuration.GetValue<string>("Auth:Issuer") ?? "retention-api";
        var audience = configuration.GetValue<string>("Auth:Audience") ?? "retention-api";
        
        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    ValidIssuer = issuer,
                    ValidAudience = audience,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey))
                };
                
                options.Events = new JwtBearerEvents
                {
                    OnChallenge = async context =>
                    {
                        // Suppress default response
                        context.HandleResponse();
                        
                        var traceId = Activity.Current?.TraceId.ToString() ?? context.HttpContext.TraceIdentifier;
                        var correlationId = context.HttpContext.Request.Headers["X-Correlation-ID"].FirstOrDefault();
                        
                        var problemDetails = new ApiProblemDetails
                        {
                            Type = "https://httpstatuses.com/401",
                            Title = "Unauthorized",
                            Status = StatusCodes.Status401Unauthorized,
                            Detail = "Authentication required. Provide a valid JWT token.",
                            Instance = context.HttpContext.Request.Path,
                            ErrorCode = ApiErrorCodes.Unauthorized,
                            TraceId = traceId,
                            CorrelationId = correlationId
                        };
                        
                        context.HttpContext.Response.StatusCode = StatusCodes.Status401Unauthorized;
                        context.HttpContext.Response.ContentType = "application/problem+json";
                        
                        await context.HttpContext.Response.WriteAsJsonAsync(problemDetails, new JsonSerializerOptions
                        {
                            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                            DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
                        });
                    },
                    OnForbidden = async context =>
                    {
                        var traceId = Activity.Current?.TraceId.ToString() ?? context.HttpContext.TraceIdentifier;
                        var correlationId = context.HttpContext.Request.Headers["X-Correlation-ID"].FirstOrDefault();
                        
                        var problemDetails = new ApiProblemDetails
                        {
                            Type = "https://httpstatuses.com/403",
                            Title = "Forbidden",
                            Status = StatusCodes.Status403Forbidden,
                            Detail = "Access denied. You do not have permission to access this resource.",
                            Instance = context.HttpContext.Request.Path,
                            ErrorCode = ApiErrorCodes.Forbidden,
                            TraceId = traceId,
                            CorrelationId = correlationId
                        };
                        
                        context.HttpContext.Response.StatusCode = StatusCodes.Status403Forbidden;
                        context.HttpContext.Response.ContentType = "application/problem+json";
                        
                        await context.HttpContext.Response.WriteAsJsonAsync(problemDetails, new JsonSerializerOptions
                        {
                            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                            DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
                        });
                    }
                };
            });
        
        services.AddAuthorization();
        
        return services;
    }
    
    /// <summary>
    /// Adds authentication/authorization middleware if enabled.
    /// </summary>
    public static IApplicationBuilder UseApiAuthentication(this IApplicationBuilder app, IConfiguration configuration)
    {
        var enabled = configuration.GetValue<bool>("Auth:Enabled");
        
        if (enabled)
        {
            app.UseAuthentication();
            app.UseAuthorization();
        }
        
        return app;
    }
}
