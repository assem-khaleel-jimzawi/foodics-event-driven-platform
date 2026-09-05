using FoodFlow.Contracts.Orders;
using FoodFlow.Contracts.Payments;
using FoodFlow.Messaging;
using MassTransit;
using Microsoft.EntityFrameworkCore;

namespace FoodFlow.Notifications.Worker;

public sealed class NotificationService(NotificationsDbContext db)
{
    public const string ConfirmedConsumer = "notifications.order-confirmed";
    public const string CancelledConsumer = "notifications.order-cancelled";
    public const string PaymentFailedConsumer = "notifications.payment-failed";

    public Task<List<NotificationRecord>> GetByOrderAsync(Guid orderId, CancellationToken cancellationToken) =>
        db.Notifications
            .AsNoTracking()
            .Where(notification => notification.OrderId == orderId)
            .OrderBy(notification => notification.CreatedAtUtc)
            .ToListAsync(cancellationToken);

    public Task HandleOrderConfirmedAsync(OrderConfirmed message, CancellationToken cancellationToken) =>
        RecordAsync(
            ConfirmedConsumer,
            message.EventId,
            message.OrderId,
            "order_confirmed",
            $"Order {message.OrderId} is confirmed. Simulated email sent.",
            cancellationToken);

    public Task HandleOrderCancelledAsync(OrderCancelled message, CancellationToken cancellationToken) =>
        RecordAsync(
            CancelledConsumer,
            message.EventId,
            message.OrderId,
            "order_cancelled",
            $"Order {message.OrderId} was cancelled: {message.Reason}. Simulated SMS sent.",
            cancellationToken);

    public Task HandlePaymentFailedAsync(PaymentFailed message, CancellationToken cancellationToken) =>
        RecordAsync(
            PaymentFailedConsumer,
            message.EventId,
            message.OrderId,
            "payment_failed",
            $"Payment for order {message.OrderId} failed: {message.Reason}. Simulated email sent.",
            cancellationToken);

    private async Task RecordAsync(
        string consumer,
        Guid eventId,
        Guid orderId,
        string type,
        string body,
        CancellationToken cancellationToken)
    {
        if (!await Inbox.TryClaimAsync(db.InboxMessages, consumer, eventId, cancellationToken))
        {
            return;
        }

        db.Notifications.Add(new NotificationRecord
        {
            Id = Guid.NewGuid(),
            OrderId = orderId,
            Type = type,
            Channel = "simulated",
            Body = body,
            CreatedAtUtc = DateTimeOffset.UtcNow
        });

        await db.SaveChangesAsync(cancellationToken);
    }
}

public sealed class OrderConfirmedNotificationConsumer(NotificationService notifications) : IConsumer<OrderConfirmed>
{
    public Task Consume(ConsumeContext<OrderConfirmed> context) =>
        notifications.HandleOrderConfirmedAsync(context.Message, context.CancellationToken);
}

public sealed class OrderCancelledNotificationConsumer(NotificationService notifications) : IConsumer<OrderCancelled>
{
    public Task Consume(ConsumeContext<OrderCancelled> context) =>
        notifications.HandleOrderCancelledAsync(context.Message, context.CancellationToken);
}

public sealed class PaymentFailedNotificationConsumer(NotificationService notifications) : IConsumer<PaymentFailed>
{
    public Task Consume(ConsumeContext<PaymentFailed> context) =>
        notifications.HandlePaymentFailedAsync(context.Message, context.CancellationToken);
}
