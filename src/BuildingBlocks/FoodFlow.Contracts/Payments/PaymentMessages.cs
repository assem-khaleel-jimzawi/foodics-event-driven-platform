namespace FoodFlow.Contracts.Payments;

/// <summary>Command: ask Payments to collect money for an order.</summary>
public sealed record ProcessPayment(
    Guid EventId,
    Guid OrderId,
    Guid CustomerId,
    decimal Amount,
    string Currency,
    DateTimeOffset OccurredAtUtc,
    Guid CorrelationId);

/// <summary>Event: money was collected (fake provider, no real PSP).</summary>
public sealed record PaymentCompleted(
    Guid EventId,
    Guid OrderId,
    Guid PaymentId,
    decimal Amount,
    string Currency,
    DateTimeOffset OccurredAtUtc,
    Guid CorrelationId);

/// <summary>Event: collection failed. Triggers compensation, not a retry of the business decision.</summary>
public sealed record PaymentFailed(
    Guid EventId,
    Guid OrderId,
    string Reason,
    DateTimeOffset OccurredAtUtc,
    Guid CorrelationId);

public sealed record RefundCompleted(
    Guid EventId,
    Guid OrderId,
    Guid PaymentId,
    decimal Amount,
    string Currency,
    DateTimeOffset OccurredAtUtc,
    Guid CorrelationId);
