namespace FoodFlow.Catalog.Application;

public sealed record CreateProductRequest(
    string Name,
    string Category,
    decimal Price,
    string Currency,
    bool IsAvailable = true);

public sealed record ProductResponse(
    Guid Id,
    string Name,
    string Category,
    decimal Price,
    string Currency,
    bool IsAvailable,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset? UpdatedAtUtc);
