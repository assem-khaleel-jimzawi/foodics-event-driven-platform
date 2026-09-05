using FoodFlow.Inventory.Domain;

namespace FoodFlow.Inventory.Application;

public interface IInventoryStore
{
    Task AddStockAsync(StockItem item, CancellationToken cancellationToken);
    Task<StockItem?> GetStockAsync(Guid productId, CancellationToken cancellationToken);
    Task AddReservationAsync(InventoryReservation reservation, CancellationToken cancellationToken);
    Task<InventoryReservation?> GetReservationAsync(Guid orderId, CancellationToken cancellationToken);
    Task<bool> TryClaimInboxAsync(string consumer, Guid messageId, CancellationToken cancellationToken);
    void Enqueue(object message, DateTimeOffset occurredAtUtc);
    Task SaveChangesAsync(CancellationToken cancellationToken);
}
