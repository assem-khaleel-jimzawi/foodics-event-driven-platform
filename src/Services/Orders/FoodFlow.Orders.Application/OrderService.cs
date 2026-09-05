using FoodFlow.Contracts.Inventory;
using FoodFlow.Contracts.Orders;
using FoodFlow.Orders.Domain;

namespace FoodFlow.Orders.Application;

public sealed class OrderService(IOrderStore store)
{
    public const string PaymentCompletedConsumer = "orders.payment-completed";
    public const string PaymentFailedConsumer = "orders.payment-failed";
    public const string InventoryFailedConsumer = "orders.inventory-failed";

    public async Task<OrderResponse> CreateAsync(
        CreateOrderRequest request,
        Guid correlationId,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var items = request.Items ?? [];
        var order = Order.Create(
            request.RestaurantId,
            request.CustomerId,
            request.Currency,
            items.Select(item => (item.ProductId, item.ProductName, item.Quantity, item.UnitPrice)).ToList());

        var now = DateTimeOffset.UtcNow;
        var created = new OrderCreated(
            Guid.NewGuid(),
            order.Id,
            order.RestaurantId,
            order.CustomerId,
            order.Items.Select(item => new OrderLine(item.ProductId, item.ProductName, item.Quantity, item.UnitPrice)).ToList(),
            order.TotalAmount,
            order.Currency,
            now,
            correlationId);

        var reserve = new ReserveInventory(
            Guid.NewGuid(),
            order.Id,
            order.RestaurantId,
            order.CustomerId,
            order.TotalAmount,
            order.Currency,
            order.Items.Select(item => new ReserveInventoryItem(item.ProductId, item.Quantity)).ToList(),
            now,
            correlationId);

        await store.AddAsync(order, cancellationToken);
        store.Enqueue(created, now);
        store.Enqueue(reserve, now);
        await store.SaveChangesAsync(cancellationToken);
        FoodFlow.Messaging.FoodFlowTelemetry.OrdersCreated.Add(1);
        return Map(order);
    }

    public async Task<OrderResponse?> GetAsync(Guid id, CancellationToken cancellationToken)
    {
        var order = await store.GetByIdAsync(id, cancellationToken);
        return order is null ? null : Map(order);
    }

    public async Task HandlePaymentCompletedAsync(Contracts.Payments.PaymentCompleted message, CancellationToken cancellationToken)
    {
        if (!await store.TryClaimInboxAsync(PaymentCompletedConsumer, message.EventId, cancellationToken))
        {
            return;
        }

        var order = await store.GetByIdAsync(message.OrderId, cancellationToken, tracked: true);
        if (order is null)
        {
            throw new Messaging.TransientMessagingException($"Order {message.OrderId} is not visible yet.");
        }

        order.Confirm();
        store.Enqueue(new OrderConfirmed(Guid.NewGuid(), order.Id, DateTimeOffset.UtcNow, message.CorrelationId));
        await store.SaveChangesAsync(cancellationToken);
        FoodFlow.Messaging.FoodFlowTelemetry.OrdersConfirmed.Add(1);
    }

    public async Task HandlePaymentFailedAsync(Contracts.Payments.PaymentFailed message, CancellationToken cancellationToken)
    {
        if (!await store.TryClaimInboxAsync(PaymentFailedConsumer, message.EventId, cancellationToken))
        {
            return;
        }

        var order = await store.GetByIdAsync(message.OrderId, cancellationToken, tracked: true);
        if (order is null)
        {
            throw new Messaging.TransientMessagingException($"Order {message.OrderId} is not visible yet.");
        }

        var now = DateTimeOffset.UtcNow;
        order.Cancel();
        store.Enqueue(new OrderCancelled(Guid.NewGuid(), order.Id, message.Reason, now, message.CorrelationId), now);
        store.Enqueue(new ReleaseInventory(Guid.NewGuid(), order.Id, now, message.CorrelationId), now);
        await store.SaveChangesAsync(cancellationToken);
        FoodFlow.Messaging.FoodFlowTelemetry.OrdersCancelled.Add(1);
    }

    public async Task HandleInventoryReservationFailedAsync(InventoryReservationFailed message, CancellationToken cancellationToken)
    {
        if (!await store.TryClaimInboxAsync(InventoryFailedConsumer, message.EventId, cancellationToken))
        {
            return;
        }

        var order = await store.GetByIdAsync(message.OrderId, cancellationToken, tracked: true);
        if (order is null)
        {
            throw new Messaging.TransientMessagingException($"Order {message.OrderId} is not visible yet.");
        }

        order.Cancel();
        store.Enqueue(new OrderCancelled(Guid.NewGuid(), order.Id, message.Reason, DateTimeOffset.UtcNow, message.CorrelationId));
        await store.SaveChangesAsync(cancellationToken);
        FoodFlow.Messaging.FoodFlowTelemetry.OrdersCancelled.Add(1);
    }

    private static OrderResponse Map(Order order) =>
        new(
            order.Id,
            order.RestaurantId,
            order.CustomerId,
            order.Status.ToString(),
            order.TotalAmount,
            order.Currency,
            order.CreatedAtUtc,
            order.UpdatedAtUtc,
            order.Items.Select(item => new OrderItemResponse(
                item.ProductId,
                item.ProductName,
                item.Quantity,
                item.UnitPrice,
                item.LineTotal)).ToList());
}
