using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.OpenApi.Models;
using Retention.Api.Auth;
using Retention.Api.Middleware;
using Retention.Api.RateLimiting;
using Retention.Api.Services;
using Retention.Application;
using Retention.Application.DependencyInjection;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
        options.JsonSerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
    });

// Configure Swagger/OpenAPI
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Release Retention API",
        Version = "v1",
        Description = "RESTful API for evaluating release retention policies and validating datasets."
    });
    
    // Include XML comments if available
    var xmlFile = $"{System.Reflection.Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
    if (File.Exists(xmlPath))
    {
        options.IncludeXmlComments(xmlPath);
    }
    
    // Add JWT Bearer authentication to Swagger
    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "JWT Authorization header using the Bearer scheme. Example: \"Authorization: Bearer {token}\"",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer"
    });
    
    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});

// Register application services
builder.Services.AddRetentionApplication();
builder.Services.AddSingleton<IRetentionEvaluator, RetentionEvaluatorAdapter>();
builder.Services.AddSingleton<IDatasetValidator, DatasetValidatorService>();

// Configure optional auth
builder.Services.AddApiAuthentication(builder.Configuration);

// Configure optional rate limiting
builder.Services.AddApiRateLimiting(builder.Configuration);

// Configure CORS for UI integration
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() 
            ?? ["http://localhost:5173"]; // Default to Vite dev server
        
        policy.WithOrigins(allowedOrigins)
            .AllowAnyMethod()
            .AllowAnyHeader()
            .AllowCredentials();
    });
});

var app = builder.Build();

// Configure the HTTP request pipeline.

// Global exception handler (must be first)
app.UseGlobalExceptionHandler();

// Swagger only in development
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/swagger/v1/swagger.json", "Release Retention API v1");
    });
}

app.UseHttpsRedirection();

// Enable CORS
app.UseCors();

// Rate limiting (if enabled)
app.UseApiRateLimiting(builder.Configuration);

// Authentication/Authorization (if enabled)
app.UseApiAuthentication(builder.Configuration);

app.MapControllers();

app.Run();

// Make Program accessible for integration tests
public partial class Program { }
