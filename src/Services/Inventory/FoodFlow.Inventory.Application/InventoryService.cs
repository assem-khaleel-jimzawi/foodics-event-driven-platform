using FoodFlow.Contracts;
using FoodFlow.Contracts.Inventory;
using FoodFlow.Inventory.Domain;
using FoodFlow.Messaging;

namespace FoodFlow.Inventory.Application;

public sealed class InventoryService(IInventoryStore store)
{
    public const string ReserveConsumer = "inventory.reserve";
    public const string ReleaseConsumer = "inventory.release";

    public async Task<StockItem> UpsertStockAsync(Guid productId, int onHand, CancellationToken cancellationToken)
    {
        var item = await store.GetStockAsync(productId, cancellationToken);
        if (item is null)
        {
            item = new StockItem(productId, onHand);
            await store.AddStockAsync(item, cancellationToken);
        }
        else
        {
            item.SetOnHand(onHand);
        }

        await store.SaveChangesAsync(cancellationToken);
        return item;
    }

    public Task<StockItem?> GetStockAsync(Guid productId, CancellationToken cancellationToken) =>
        store.GetStockAsync(productId, cancellationToken);

    public Task<InventoryReservation?> GetReservationAsync(Guid orderId, CancellationToken cancellationToken) =>
        store.GetReservationAsync(orderId, cancellationToken);

    public async Task HandleReserveAsync(ReserveInventory message, CancellationToken cancellationToken)
    {
        if (message.Items is null || message.Items.Count == 0)
        {
            throw new PermanentMessagingException("ReserveInventory contained no items.");
        }

        if (message.Items.Any(item => item.ProductId == DemoScenarios.PermanentFailureProductId))
        {
            throw new PermanentMessagingException("ReserveInventory referenced a permanently invalid product.");
        }

        if (!await store.TryClaimInboxAsync(ReserveConsumer, message.EventId, cancellationToken))
        {
            return;
        }

        if (message.Items.Any(item => item.ProductId == DemoScenarios.TransientFailureProductId))
        {
            TransientFailureGate.ThrowUntilSuccessful(message.EventId);
        }

        var existing = await store.GetReservationAsync(message.OrderId, cancellationToken);
        if (existing is not null)
        {
            await store.SaveChangesAsync(cancellationToken);
            return;
        }

        var now = DateTimeOffset.UtcNow;
        try
        {
            var toReserve = new List<(StockItem Stock, int Quantity)>();
            foreach (var line in message.Items)
            {
                var stock = await store.GetStockAsync(line.ProductId, cancellationToken)
                    ?? throw new DomainException("unknown_product", $"No stock record for {line.ProductId}.");
                if (stock.Available < line.Quantity)
                {
                    throw new DomainException("insufficient_stock", $"Not enough stock for product {line.ProductId}.");
                }

                toReserve.Add((stock, line.Quantity));
            }

            foreach (var (stock, quantity) in toReserve)
            {
                stock.Reserve(quantity);
            }

            var reservation = InventoryReservation.Create(
                message.OrderId,
                message.Items.Select(item => (item.ProductId, item.Quantity)).ToList());
            await store.AddReservationAsync(reservation, cancellationToken);

            var reserved = new InventoryReserved(
                Guid.NewGuid(),
                message.OrderId,
                message.RestaurantId,
                message.CustomerId,
                message.Amount,
                message.Currency,
                message.Items,
                now,
                message.CorrelationId);

            store.Enqueue(reserved, now);
            await store.SaveChangesAsync(cancellationToken);
            FoodFlowTelemetry.InventoryReservations.Add(1);
        }
        catch (DomainException exception)
        {
            store.Enqueue(
                new InventoryReservationFailed(Guid.NewGuid(), message.OrderId, exception.Message, now, message.CorrelationId),
                now);
            await store.SaveChangesAsync(cancellationToken);
            FoodFlowTelemetry.InventoryReservationFailures.Add(1);
        }
    }

    public async Task HandleReleaseAsync(ReleaseInventory message, CancellationToken cancellationToken)
    {
        if (!await store.TryClaimInboxAsync(ReleaseConsumer, message.EventId, cancellationToken))
        {
            return;
        }

        var reservation = await store.GetReservationAsync(message.OrderId, cancellationToken);
        if (reservation is null || reservation.Status == ReservationStatus.Released)
        {
            await store.SaveChangesAsync(cancellationToken);
            return;
        }

        foreach (var line in reservation.Lines)
        {
            var stock = await store.GetStockAsync(line.ProductId, cancellationToken);
            stock?.Release(line.Quantity);
        }

        reservation.Release();
        var now = DateTimeOffset.UtcNow;
        store.Enqueue(new InventoryReleased(Guid.NewGuid(), message.OrderId, now, message.CorrelationId), now);
        await store.SaveChangesAsync(cancellationToken);
    }
}

internal static class TransientFailureGate
{
    private static readonly System.Collections.Concurrent.ConcurrentDictionary<Guid, int> Attempts = new();

    public static void ThrowUntilSuccessful(Guid commandId)
    {
        var attempt = Attempts.AddOrUpdate(commandId, 1, (_, current) => current + 1);
        if (attempt < 3)
        {
            throw new TransientMessagingException($"Simulated transient inventory failure attempt {attempt} for {commandId}.");
        }
    }
}
