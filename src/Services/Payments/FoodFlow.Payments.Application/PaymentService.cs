using FoodFlow.Contracts;
using FoodFlow.Contracts.Inventory;
using FoodFlow.Contracts.Payments;
using FoodFlow.Messaging;
using FoodFlow.Payments.Domain;

namespace FoodFlow.Payments.Application;

public interface IPaymentStore
{
    Task<Payment?> GetByOrderIdAsync(Guid orderId, CancellationToken cancellationToken);
    Task AddAsync(Payment payment, CancellationToken cancellationToken);
    Task<bool> TryClaimInboxAsync(string consumer, Guid messageId, CancellationToken cancellationToken);
    void Enqueue(object message, DateTimeOffset occurredAtUtc);
    Task SaveChangesAsync(CancellationToken cancellationToken);
}

public interface IPaymentGateway
{
    PaymentDecision Charge(Guid customerId, decimal amount, string currency);
}

public sealed record PaymentDecision(bool Succeeded, string? FailureReason);

public sealed class FakePaymentGateway : IPaymentGateway
{
    public PaymentDecision Charge(Guid customerId, decimal amount, string currency)
    {
        if (amount <= 0)
        {
            return new PaymentDecision(false, "Amount must be greater than zero.");
        }

        if (customerId == DemoScenarios.PaymentFailCustomerId)
        {
            return new PaymentDecision(false, "Fake provider declined the card.");
        }

        return new PaymentDecision(true, null);
    }
}

public sealed class PaymentService(IPaymentStore store, IPaymentGateway gateway)
{
    public const string InventoryReservedConsumer = "payments.inventory-reserved";

    public Task<Payment?> GetByOrderIdAsync(Guid orderId, CancellationToken cancellationToken) =>
        store.GetByOrderIdAsync(orderId, cancellationToken);

    public async Task HandleInventoryReservedAsync(InventoryReserved message, CancellationToken cancellationToken)
    {
        if (!await store.TryClaimInboxAsync(InventoryReservedConsumer, message.EventId, cancellationToken))
        {
            return;
        }

        var existing = await store.GetByOrderIdAsync(message.OrderId, cancellationToken);
        if (existing is not null)
        {
            await store.SaveChangesAsync(cancellationToken);
            return;
        }

        var decision = gateway.Charge(message.CustomerId, message.Amount, message.Currency);
        var now = DateTimeOffset.UtcNow;

        if (decision.Succeeded)
        {
            var payment = Payment.Completed(message.OrderId, message.Amount, message.Currency);
            await store.AddAsync(payment, cancellationToken);
            store.Enqueue(
                new PaymentCompleted(
                    Guid.NewGuid(),
                    message.OrderId,
                    payment.Id,
                    payment.Amount,
                    payment.Currency,
                    now,
                    message.CorrelationId),
                now);
        }
        else
        {
            var payment = Payment.Failed(message.OrderId, message.Amount, message.Currency, decision.FailureReason ?? "declined");
            await store.AddAsync(payment, cancellationToken);
            store.Enqueue(
                new PaymentFailed(
                    Guid.NewGuid(),
                    message.OrderId,
                    payment.FailureReason ?? "declined",
                    now,
                    message.CorrelationId),
                now);
        }

        await store.SaveChangesAsync(cancellationToken);
    }
}
