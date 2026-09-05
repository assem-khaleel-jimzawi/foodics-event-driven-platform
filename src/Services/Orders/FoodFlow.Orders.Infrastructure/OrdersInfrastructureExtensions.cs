using FoodFlow.Contracts.Inventory;
using FoodFlow.Contracts.Payments;
using FoodFlow.Messaging;
using FoodFlow.Orders.Application;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace FoodFlow.Orders.Infrastructure;

public sealed class PaymentCompletedConsumer(OrderService orders) : IConsumer<PaymentCompleted>
{
    public Task Consume(ConsumeContext<PaymentCompleted> context) =>
        orders.HandlePaymentCompletedAsync(context.Message, context.CancellationToken);
}

public sealed class PaymentFailedConsumer(OrderService orders) : IConsumer<PaymentFailed>
{
    public Task Consume(ConsumeContext<PaymentFailed> context) =>
        orders.HandlePaymentFailedAsync(context.Message, context.CancellationToken);
}

public sealed class InventoryReservationFailedConsumer(OrderService orders) : IConsumer<InventoryReservationFailed>
{
    public Task Consume(ConsumeContext<InventoryReservationFailed> context) =>
        orders.HandleInventoryReservationFailedAsync(context.Message, context.CancellationToken);
}

public static class OrdersInfrastructureExtensions
{
    public static IHostApplicationBuilder AddOrdersInfrastructure(this IHostApplicationBuilder builder)
    {
        var connectionString = builder.Configuration.GetConnectionString("OrdersDb")
            ?? throw new InvalidOperationException("Connection string 'OrdersDb' is not configured.");

        builder.Services.AddDbContext<OrdersDbContext>(options => options.UseNpgsql(connectionString));
        builder.Services.AddScoped<IOrderStore, OrderStore>();
        builder.Services.AddScoped<OrderService>();
        builder.Services.AddHealthChecks().AddDbContextCheck<OrdersDbContext>("orders-db", tags: ["ready"]);
        builder.Services.AddOutboxProcessor<OrdersDbContext>();
        return builder;
    }
}
