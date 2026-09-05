using System.Diagnostics;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace FoodFlow.ServiceDefaults;

public sealed record FoodFlowServiceInfo(string ServiceName, int DeliveryPhase);

public static class FoodFlowServiceDefaults
{
    public static IHostApplicationBuilder AddFoodFlowServiceDefaults(
        this IHostApplicationBuilder builder,
        string serviceName,
        int deliveryPhase = 1)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(serviceName);

        builder.Services.AddSingleton(new FoodFlowServiceInfo(serviceName, deliveryPhase));

        builder.Services.AddProblemDetails(options =>
        {
            options.CustomizeProblemDetails = context =>
            {
                context.ProblemDetails.Extensions["service"] = serviceName;
                context.ProblemDetails.Extensions["traceId"] = Activity.Current?.Id ?? context.HttpContext.TraceIdentifier;
            };
        });

        builder.Services.AddOpenApi();
        builder.Services.ConfigureHttpJsonOptions(options =>
        {
            options.SerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
            options.SerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
            options.SerializerOptions.Converters.Add(new JsonStringEnumConverter());
        });

        builder.Logging.AddJsonConsole(options =>
        {
            options.IncludeScopes = true;
            options.UseUtcTimestamp = true;
            options.TimestampFormat = "O";
            options.JsonWriterOptions = new JsonWriterOptions { Indented = false };
        });

        builder.Logging.Configure(options =>
        {
            options.ActivityTrackingOptions = ActivityTrackingOptions.TraceId | ActivityTrackingOptions.SpanId;
        });

        builder.Services.AddHealthChecks()
            .AddCheck("self", () => HealthCheckResult.Healthy(), tags: ["live"]);

        return builder;
    }

    public static IHostApplicationBuilder AddFoodFlowFallbackExceptionHandler(this IHostApplicationBuilder builder)
    {
        builder.Services.AddExceptionHandler<FoodFlowExceptionHandler>();
        return builder;
    }

    public static WebApplication MapFoodFlowServiceDefaults(this WebApplication app)
    {
        var info = app.Services.GetRequiredService<FoodFlowServiceInfo>();

        app.UseExceptionHandler();
        app.UseStatusCodePages();
        app.UseMiddleware<CorrelationIdMiddleware>();

        if (!app.Environment.IsDevelopment())
        {
            app.UseHttpsRedirection();
        }

        app.MapOpenApi();

        app.MapHealthChecks("/health/live", new HealthCheckOptions
        {
            Predicate = check => check.Tags.Contains("live"),
            ResponseWriter = WriteHealthResponse
        });

        app.MapHealthChecks("/health/ready", new HealthCheckOptions
        {
            Predicate = check => check.Tags.Contains("ready") || check.Tags.Contains("live"),
            ResponseWriter = WriteHealthResponse
        });

        app.MapGet("/", () => Results.Ok(new
        {
            service = info.ServiceName,
            deliveryPhase = info.DeliveryPhase,
            description = "FoodFlow restaurant platform"
        }));

        return app;
    }

    private static async Task WriteHealthResponse(HttpContext context, HealthReport report)
    {
        context.Response.ContentType = "application/json";
        var payload = new
        {
            status = report.Status.ToString(),
            durationMs = report.TotalDuration.TotalMilliseconds,
            checks = report.Entries.Select(entry => new
            {
                name = entry.Key,
                status = entry.Value.Status.ToString(),
                durationMs = entry.Value.Duration.TotalMilliseconds,
                description = entry.Value.Description
            })
        };

        await context.Response.WriteAsJsonAsync(payload);
    }
}
