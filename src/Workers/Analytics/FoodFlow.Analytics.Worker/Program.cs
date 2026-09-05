using FoodFlow.Analytics.Worker;
using FoodFlow.Messaging;
using FoodFlow.ServiceDefaults;

var builder = WebApplication.CreateBuilder(args);

builder.AddFoodFlowServiceDefaults("foodflow-analytics", deliveryPhase: 2);
builder.AddFoodFlowFallbackExceptionHandler();
builder.Services.Configure<KafkaOptions>(builder.Configuration.GetSection(KafkaOptions.SectionName));
builder.Services.AddHostedService<KafkaTopicProvisioner>();
builder.Services.AddSingleton<AnalyticsStore>();
builder.Services.AddHostedService<KafkaAnalyticsConsumer>();
builder.Services.AddHealthChecks().AddCheck("kafka-consumer", () =>
    Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckResult.Healthy("Analytics consumer loop is hosted."), tags: ["ready"]);

var app = builder.Build();
app.MapFoodFlowServiceDefaults();
app.MapGet("/api/analytics/snapshot", (AnalyticsStore store) => Results.Ok(store.GetSnapshot()));
app.Run();

public partial class Program;

namespace FoodFlow.Analytics.Worker
{
    public sealed class AssemblyMarker;
}
