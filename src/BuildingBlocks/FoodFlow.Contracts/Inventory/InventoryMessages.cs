namespace FoodFlow.Contracts.Inventory;

/// <summary>Command: ask Inventory to reserve stock for an order.</summary>
public sealed record ReserveInventory(
    Guid EventId,
    Guid OrderId,
    Guid RestaurantId,
    IReadOnlyList<ReserveInventoryItem> Items,
    DateTimeOffset OccurredAtUtc,
    Guid CorrelationId);

public sealed record ReserveInventoryItem(Guid ProductId, int Quantity);

/// <summary>Command: release a previous reservation after payment/order failure.</summary>
public sealed record ReleaseInventory(
    Guid EventId,
    Guid OrderId,
    DateTimeOffset OccurredAtUtc,
    Guid CorrelationId);

public sealed record InventoryReserved(
    Guid EventId,
    Guid OrderId,
    DateTimeOffset OccurredAtUtc,
    Guid CorrelationId);

public sealed record InventoryReservationFailed(
    Guid EventId,
    Guid OrderId,
    string Reason,
    DateTimeOffset OccurredAtUtc,
    Guid CorrelationId);

public sealed record InventoryReleased(
    Guid EventId,
    Guid OrderId,
    DateTimeOffset OccurredAtUtc,
    Guid CorrelationId);
