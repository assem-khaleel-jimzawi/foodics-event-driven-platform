namespace FoodFlow.Contracts.Orders;

public sealed record OrderLine(
    Guid ProductId,
    string ProductName,
    int Quantity,
    decimal UnitPrice);

/// <summary>Event: an order was persisted. Fact, not a request.</summary>
public sealed record OrderCreated(
    Guid EventId,
    Guid OrderId,
    Guid RestaurantId,
    Guid CustomerId,
    IReadOnlyList<OrderLine> Items,
    decimal TotalAmount,
    string Currency,
    DateTimeOffset OccurredAtUtc,
    Guid CorrelationId);

/// <summary>Event: the order completed the happy path (inventory + payment).</summary>
public sealed record OrderConfirmed(
    Guid EventId,
    Guid OrderId,
    DateTimeOffset OccurredAtUtc,
    Guid CorrelationId);

/// <summary>Event: the order was cancelled, including compensating cancellations.</summary>
public sealed record OrderCancelled(
    Guid EventId,
    Guid OrderId,
    string Reason,
    DateTimeOffset OccurredAtUtc,
    Guid CorrelationId);
