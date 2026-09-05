using FoodFlow.Messaging;
using FoodFlow.Payments.Application;
using FoodFlow.Payments.Infrastructure;
using FoodFlow.ServiceDefaults;
using MassTransit;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.AddFoodFlowServiceDefaults("foodflow-payments", deliveryPhase: 2);
builder.AddFoodFlowFallbackExceptionHandler();
builder.AddPaymentsInfrastructure();
builder.AddFoodFlowRabbitMq(bus => bus.AddConsumer<InventoryReservedConsumer>());

var app = builder.Build();

if (app.Environment.IsDevelopment() || app.Configuration.GetValue("Database:MigrateOnStartup", false))
{
    await FoodFlowServiceDefaults.RetryStartupAsync(async () =>
    {
        await using var scope = app.Services.CreateAsyncScope();
        await scope.ServiceProvider.GetRequiredService<PaymentsDbContext>().Database.MigrateAsync();
    });
}

app.MapFoodFlowServiceDefaults();
app.MapGet("/api/payments/by-order/{orderId:guid}", async (Guid orderId, PaymentService payments, CancellationToken cancellationToken) =>
{
    var payment = await payments.GetByOrderIdAsync(orderId, cancellationToken);
    return payment is null
        ? ProblemDetailsHttp.NotFound("Payment not found", $"No payment for order {orderId}.", "orderId", orderId)
        : Results.Ok(new
        {
            payment.Id,
            payment.OrderId,
            payment.Amount,
            payment.Currency,
            status = payment.Status.ToString(),
            payment.TransactionId,
            payment.FailureReason
        });
});

app.Run();

public partial class Program;

namespace FoodFlow.Payments.Api
{
    public sealed class AssemblyMarker;
}
