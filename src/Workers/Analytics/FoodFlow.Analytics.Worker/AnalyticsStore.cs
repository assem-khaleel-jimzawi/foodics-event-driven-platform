using System.Collections.Concurrent;
using FoodFlow.Contracts.Inventory;
using FoodFlow.Contracts.Orders;
using FoodFlow.Contracts.Payments;

namespace FoodFlow.Analytics.Worker;

public sealed class AnalyticsSnapshot
{
    public int OrdersCreated { get; set; }
    public int OrdersConfirmed { get; set; }
    public int OrdersCancelled { get; set; }
    public int PaymentsCompleted { get; set; }
    public int PaymentsFailed { get; set; }
    public int InventoryFailures { get; set; }
    public decimal Revenue { get; set; }
    public string Currency { get; set; } = "SAR";
    public string? LastEventType { get; set; }
    public DateTimeOffset? LastEventAtUtc { get; set; }
    public long EventsConsumed { get; set; }
}

public sealed class AnalyticsStore
{
    private readonly object _gate = new();
    private readonly AnalyticsSnapshot _snapshot = new();
    private readonly ConcurrentDictionary<Guid, byte> _seen = new();

    public AnalyticsSnapshot GetSnapshot()
    {
        lock (_gate)
        {
            return new AnalyticsSnapshot
            {
                OrdersCreated = _snapshot.OrdersCreated,
                OrdersConfirmed = _snapshot.OrdersConfirmed,
                OrdersCancelled = _snapshot.OrdersCancelled,
                PaymentsCompleted = _snapshot.PaymentsCompleted,
                PaymentsFailed = _snapshot.PaymentsFailed,
                InventoryFailures = _snapshot.InventoryFailures,
                Revenue = _snapshot.Revenue,
                Currency = _snapshot.Currency,
                LastEventType = _snapshot.LastEventType,
                LastEventAtUtc = _snapshot.LastEventAtUtc,
                EventsConsumed = _snapshot.EventsConsumed
            };
        }
    }

    public void Apply(string messageType, object body)
    {
        lock (_gate)
        {
            if (body is OrderCreated created && Mark(created.EventId))
            {
                _snapshot.OrdersCreated++;
            }
            else if (body is OrderConfirmed confirmed && Mark(confirmed.EventId))
            {
                _snapshot.OrdersConfirmed++;
            }
            else if (body is OrderCancelled cancelled && Mark(cancelled.EventId))
            {
                _snapshot.OrdersCancelled++;
            }
            else if (body is PaymentCompleted paid && Mark(paid.EventId))
            {
                _snapshot.PaymentsCompleted++;
                _snapshot.Revenue += paid.Amount;
                _snapshot.Currency = paid.Currency;
            }
            else if (body is PaymentFailed failed && Mark(failed.EventId))
            {
                _snapshot.PaymentsFailed++;
            }
            else if (body is InventoryReservationFailed inventoryFailed && Mark(inventoryFailed.EventId))
            {
                _snapshot.InventoryFailures++;
            }
            else if (body is InventoryReserved reserved)
            {
                Mark(reserved.EventId);
            }

            _snapshot.LastEventType = messageType;
            _snapshot.LastEventAtUtc = DateTimeOffset.UtcNow;
            _snapshot.EventsConsumed++;
        }
    }

    private bool Mark(Guid eventId) => _seen.TryAdd(eventId, 0);
}
