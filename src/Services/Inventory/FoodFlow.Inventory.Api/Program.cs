using FoodFlow.Inventory.Application;
using FoodFlow.Inventory.Infrastructure;
using FoodFlow.Messaging;
using FoodFlow.ServiceDefaults;
using MassTransit;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.AddFoodFlowServiceDefaults("foodflow-inventory", deliveryPhase: 3);
builder.AddFoodFlowFallbackExceptionHandler();
builder.AddInventoryInfrastructure();
builder.AddFoodFlowRabbitMq(bus =>
{
    bus.AddConsumer<ReserveInventoryConsumer>();
    bus.AddConsumer<ReleaseInventoryConsumer>();
});

var app = builder.Build();

if (app.Environment.IsDevelopment() || app.Configuration.GetValue("Database:MigrateOnStartup", false))
{
    await FoodFlowServiceDefaults.RetryStartupAsync(async () =>
    {
        await using var scope = app.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<InventoryDbContext>();
        await db.Database.MigrateAsync();
        await db.SeedDemoStockAsync();
    });
}

app.MapFoodFlowServiceDefaults();

var stock = app.MapGroup("/api/inventory").WithTags("Inventory");
stock.MapPost("/stock", async (UpsertStockRequest request, InventoryService inventory, CancellationToken cancellationToken) =>
{
    var item = await inventory.UpsertStockAsync(request.ProductId, request.OnHand, cancellationToken);
    return TypedResults.Ok(new { item.ProductId, item.OnHand, item.Reserved, item.Available });
});
stock.MapGet("/stock/{productId:guid}", async (Guid productId, InventoryService inventory, CancellationToken cancellationToken) =>
{
    var item = await inventory.GetStockAsync(productId, cancellationToken);
    return item is null
        ? ProblemDetailsHttp.NotFound("Stock not found", $"No stock for {productId}.", "productId", productId)
        : Results.Ok(new { item.ProductId, item.OnHand, item.Reserved, item.Available });
});
stock.MapGet("/reservations/{orderId:guid}", async (Guid orderId, InventoryService inventory, CancellationToken cancellationToken) =>
{
    var reservation = await inventory.GetReservationAsync(orderId, cancellationToken);
    return reservation is null
        ? ProblemDetailsHttp.NotFound("Reservation not found", $"No reservation for order {orderId}.", "orderId", orderId)
        : Results.Ok(new
        {
            reservation.OrderId,
            status = reservation.Status.ToString(),
            reservation.CreatedAtUtc,
            reservation.ReleasedAtUtc,
            lines = reservation.Lines.Select(line => new { line.ProductId, line.Quantity })
        });
});

app.Run();

public sealed record UpsertStockRequest(Guid ProductId, int OnHand);

public partial class Program;

namespace FoodFlow.Inventory.Api
{
    public sealed class AssemblyMarker;
}
