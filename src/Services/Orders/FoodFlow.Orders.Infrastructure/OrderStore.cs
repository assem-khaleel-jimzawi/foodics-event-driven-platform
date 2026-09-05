using FoodFlow.Messaging;
using FoodFlow.Orders.Application;
using FoodFlow.Orders.Domain;
using Microsoft.EntityFrameworkCore;

namespace FoodFlow.Orders.Infrastructure;

public sealed class OrderStore(OrdersDbContext db) : IOrderStore
{
    public Task AddAsync(Order order, CancellationToken cancellationToken) =>
        db.Orders.AddAsync(order, cancellationToken).AsTask();

    public async Task<Order?> GetByIdAsync(Guid id, CancellationToken cancellationToken, bool tracked = false)
    {
        IQueryable<Order> query = db.Orders.Include(order => order.Items);
        if (!tracked)
        {
            query = query.AsNoTracking();
        }

        return await query.FirstOrDefaultAsync(order => order.Id == id, cancellationToken);
    }

    public Task<bool> TryClaimInboxAsync(string consumer, Guid messageId, CancellationToken cancellationToken) =>
        Inbox.TryClaimAsync(db.InboxMessages, consumer, messageId, cancellationToken);

    public void Enqueue(object message, DateTimeOffset? occurredAtUtc = null) =>
        db.AddOperational(message, occurredAtUtc);

    public Task SaveChangesAsync(CancellationToken cancellationToken) =>
        db.SaveChangesAsync(cancellationToken);
}
