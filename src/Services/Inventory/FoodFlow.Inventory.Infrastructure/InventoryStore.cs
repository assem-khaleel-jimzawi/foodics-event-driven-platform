using FoodFlow.Inventory.Application;
using FoodFlow.Inventory.Domain;
using FoodFlow.Messaging;
using Microsoft.EntityFrameworkCore;

namespace FoodFlow.Inventory.Infrastructure;

public sealed class InventoryStore(InventoryDbContext db) : IInventoryStore
{
    public Task AddStockAsync(StockItem item, CancellationToken cancellationToken)
    {
        db.StockItems.Add(item);
        return Task.CompletedTask;
    }

    public Task<StockItem?> GetStockAsync(Guid productId, CancellationToken cancellationToken) =>
        db.StockItems.FirstOrDefaultAsync(item => item.ProductId == productId, cancellationToken);

    public Task AddReservationAsync(InventoryReservation reservation, CancellationToken cancellationToken)
    {
        db.Reservations.Add(reservation);
        return Task.CompletedTask;
    }

    public Task<InventoryReservation?> GetReservationAsync(Guid orderId, CancellationToken cancellationToken) =>
        db.Reservations
            .Include(reservation => reservation.Lines)
            .FirstOrDefaultAsync(reservation => reservation.OrderId == orderId, cancellationToken);

    public Task<bool> TryClaimInboxAsync(string consumer, Guid messageId, CancellationToken cancellationToken) =>
        Inbox.TryClaimAsync(db.InboxMessages, consumer, messageId, cancellationToken);

    public void Enqueue(object message, DateTimeOffset occurredAtUtc) =>
        db.AddOperational(message, occurredAtUtc);

    public async Task SaveChangesAsync(CancellationToken cancellationToken)
    {
        try
        {
            await db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException exception)
        {
            throw new TransientMessagingException("Concurrent stock update; the message will be retried.", exception);
        }
    }
}
