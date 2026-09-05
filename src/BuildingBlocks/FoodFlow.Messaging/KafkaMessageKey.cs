using FoodFlow.Contracts.Catalog;
using FoodFlow.Contracts.Inventory;
using FoodFlow.Contracts.Orders;
using FoodFlow.Contracts.Payments;

namespace FoodFlow.Messaging;

/// <summary>
/// Kafka partition key is the order id when present so an order's facts stay ordered on one partition.
/// </summary>
public static class KafkaMessageKey
{
    public static string For(object message) => message switch
    {
        OrderCreated value => value.OrderId.ToString("D"),
        OrderConfirmed value => value.OrderId.ToString("D"),
        OrderCancelled value => value.OrderId.ToString("D"),
        InventoryReserved value => value.OrderId.ToString("D"),
        InventoryReservationFailed value => value.OrderId.ToString("D"),
        InventoryReleased value => value.OrderId.ToString("D"),
        PaymentCompleted value => value.OrderId.ToString("D"),
        PaymentFailed value => value.OrderId.ToString("D"),
        ProductCreated value => value.ProductId.ToString("D"),
        _ => Guid.NewGuid().ToString("D")
    };
}
