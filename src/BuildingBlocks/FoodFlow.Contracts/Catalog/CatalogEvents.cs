namespace FoodFlow.Contracts.Catalog;

public sealed record ProductCreated(
    Guid EventId,
    Guid ProductId,
    string Name,
    string Category,
    decimal Price,
    string Currency,
    bool IsAvailable,
    DateTimeOffset OccurredAtUtc,
    Guid CorrelationId);

public sealed record ProductPriceChanged(
    Guid EventId,
    Guid ProductId,
    decimal PreviousPrice,
    decimal NewPrice,
    string Currency,
    DateTimeOffset OccurredAtUtc,
    Guid CorrelationId);

public sealed record ProductAvailabilityChanged(
    Guid EventId,
    Guid ProductId,
    bool IsAvailable,
    DateTimeOffset OccurredAtUtc,
    Guid CorrelationId);
