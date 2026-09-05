using FoodFlow.Messaging;
using FoodFlow.Notifications.Worker;
using FoodFlow.ServiceDefaults;
using MassTransit;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.AddFoodFlowServiceDefaults("foodflow-notifications", deliveryPhase: 2);
builder.AddFoodFlowFallbackExceptionHandler();

var connectionString = builder.Configuration.GetConnectionString("NotificationsDb")
    ?? throw new InvalidOperationException("Connection string 'NotificationsDb' is not configured.");

builder.Services.AddDbContext<NotificationsDbContext>(options => options.UseNpgsql(connectionString));
builder.Services.AddScoped<NotificationService>();
builder.Services.AddHealthChecks().AddDbContextCheck<NotificationsDbContext>("notifications-db", tags: ["ready"]);
builder.AddFoodFlowRabbitMq(bus =>
{
    bus.AddConsumer<OrderConfirmedNotificationConsumer>();
    bus.AddConsumer<OrderCancelledNotificationConsumer>();
    bus.AddConsumer<PaymentFailedNotificationConsumer>();
});

var app = builder.Build();

if (app.Environment.IsDevelopment() || app.Configuration.GetValue("Database:MigrateOnStartup", false))
{
    await FoodFlowServiceDefaults.RetryStartupAsync(async () =>
    {
        await using var scope = app.Services.CreateAsyncScope();
        await scope.ServiceProvider.GetRequiredService<NotificationsDbContext>().Database.MigrateAsync();
    });
}

app.MapFoodFlowServiceDefaults();
app.MapGet("/api/notifications/by-order/{orderId:guid}", async (Guid orderId, NotificationService notifications, CancellationToken cancellationToken) =>
{
    var items = await notifications.GetByOrderAsync(orderId, cancellationToken);
    return Results.Ok(items.Select(item => new
    {
        item.Id,
        item.OrderId,
        item.Type,
        item.Channel,
        item.Body,
        item.CreatedAtUtc
    }));
});

app.Run();

public partial class Program;

namespace FoodFlow.Notifications.Worker
{
    public sealed class AssemblyMarker;
}
