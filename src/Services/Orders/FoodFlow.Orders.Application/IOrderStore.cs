using FoodFlow.Orders.Domain;

namespace FoodFlow.Orders.Application;

public interface IOrderStore
{
    Task AddAsync(Order order, CancellationToken cancellationToken);
    Task<Order?> GetByIdAsync(Guid id, CancellationToken cancellationToken, bool tracked = false);
    Task<bool> TryClaimInboxAsync(string consumer, Guid messageId, CancellationToken cancellationToken);
    void Enqueue(object message, DateTimeOffset? occurredAtUtc = null);
    Task SaveChangesAsync(CancellationToken cancellationToken);
}
