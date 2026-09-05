namespace FoodFlow.Contracts.Inventory;

public sealed record ReserveInventoryItem(Guid ProductId, int Quantity);

/// <summary>Command: ask Inventory to reserve stock for an order.</summary>
public sealed record ReserveInventory(
    Guid EventId,
    Guid OrderId,
    Guid RestaurantId,
    Guid CustomerId,
    decimal Amount,
    string Currency,
    IReadOnlyList<ReserveInventoryItem> Items,
    DateTimeOffset OccurredAtUtc,
    Guid CorrelationId);

/// <summary>Command: release a previous reservation after payment/order failure.</summary>
public sealed record ReleaseInventory(
    Guid EventId,
    Guid OrderId,
    DateTimeOffset OccurredAtUtc,
    Guid CorrelationId);

/// <summary>Event: stock was reserved for an order.</summary>
public sealed record InventoryReserved(
    Guid EventId,
    Guid OrderId,
    Guid RestaurantId,
    Guid CustomerId,
    decimal Amount,
    string Currency,
    IReadOnlyList<ReserveInventoryItem> Items,
    DateTimeOffset OccurredAtUtc,
    Guid CorrelationId);

/// <summary>Event: stock could not be reserved.</summary>
public sealed record InventoryReservationFailed(
    Guid EventId,
    Guid OrderId,
    string Reason,
    DateTimeOffset OccurredAtUtc,
    Guid CorrelationId);

/// <summary>Event: a reservation was released (compensation).</summary>
public sealed record InventoryReleased(
    Guid EventId,
    Guid OrderId,
    DateTimeOffset OccurredAtUtc,
    Guid CorrelationId);
