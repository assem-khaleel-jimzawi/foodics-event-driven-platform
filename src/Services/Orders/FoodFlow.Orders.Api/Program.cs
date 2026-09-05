using FoodFlow.Messaging;
using FoodFlow.Orders.Api;
using FoodFlow.Orders.Infrastructure;
using FoodFlow.ServiceDefaults;
using MassTransit;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.AddFoodFlowServiceDefaults("foodflow-orders", deliveryPhase: 2);
builder.Services.AddExceptionHandler<OrdersExceptionHandler>();
builder.AddFoodFlowFallbackExceptionHandler();
builder.AddOrdersInfrastructure();
builder.AddFoodFlowRabbitMq(bus =>
{
    bus.AddConsumer<PaymentCompletedConsumer>();
    bus.AddConsumer<PaymentFailedConsumer>();
    bus.AddConsumer<InventoryReservationFailedConsumer>();
});

var app = builder.Build();

if (app.Environment.IsDevelopment() || app.Configuration.GetValue("Database:MigrateOnStartup", false))
{
    await FoodFlowServiceDefaults.RetryStartupAsync(async () =>
    {
        await using var scope = app.Services.CreateAsyncScope();
        await scope.ServiceProvider.GetRequiredService<OrdersDbContext>().Database.MigrateAsync();
    });
}

app.MapFoodFlowServiceDefaults();
app.MapOrderEndpoints();
app.Run();

public partial class Program;

namespace FoodFlow.Orders.Api
{
    public sealed class AssemblyMarker;
}
