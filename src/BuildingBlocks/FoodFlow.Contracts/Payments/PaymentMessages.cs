namespace FoodFlow.Contracts.Payments;

/// <summary>Command: ask Payments to collect money for an order.</summary>
public sealed record ProcessPayment(
    Guid EventId,
    Guid OrderId,
    decimal Amount,
    string Currency,
    DateTimeOffset OccurredAtUtc,
    Guid CorrelationId);

public sealed record PaymentCompleted(
    Guid EventId,
    Guid OrderId,
    Guid PaymentId,
    decimal Amount,
    string Currency,
    DateTimeOffset OccurredAtUtc,
    Guid CorrelationId);

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
