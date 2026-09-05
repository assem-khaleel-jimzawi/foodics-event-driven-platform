namespace FoodFlow.ServiceDefaults;

/// <summary>
/// Copies or assigns X-Correlation-Id so logs across a single client request can be joined
/// even before OpenTelemetry is wired in Phase 3. CorrelationId is a business/request id;
/// TraceId is a telemetry id. They often travel together but are not the same thing.
/// </summary>
public sealed class CorrelationIdMiddleware(RequestDelegate next)
{
    public const string HeaderName = "X-Correlation-Id";

    public async Task InvokeAsync(HttpContext context, ILogger<CorrelationIdMiddleware> logger)
    {
        var correlationId = context.Request.Headers[HeaderName].FirstOrDefault();
        if (string.IsNullOrWhiteSpace(correlationId))
        {
            correlationId = Guid.NewGuid().ToString("D");
        }

        context.Items[HeaderName] = correlationId;
        context.Response.Headers[HeaderName] = correlationId;

        using (logger.BeginScope(new Dictionary<string, object>
        {
            ["CorrelationId"] = correlationId,
            ["ServiceName"] = context.RequestServices.GetService<FoodFlowServiceInfo>()?.ServiceName ?? "unknown"
        }))
        {
            await next(context);
        }
    }
}
