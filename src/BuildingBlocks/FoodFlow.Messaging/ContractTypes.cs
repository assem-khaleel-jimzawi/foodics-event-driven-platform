using FoodFlow.Contracts.Catalog;
using FoodFlow.Contracts.Inventory;
using FoodFlow.Contracts.Orders;
using FoodFlow.Contracts.Payments;

namespace FoodFlow.Messaging;

public static class ContractTypes
{
    private static readonly IReadOnlyDictionary<string, Type> Types = typeof(OrderCreated).Assembly
        .GetTypes()
        .Where(type => type.IsClass && type.IsSealed && type.Namespace?.StartsWith("FoodFlow.Contracts", StringComparison.Ordinal) == true)
        .ToDictionary(type => type.FullName!, type => type, StringComparer.Ordinal);

    public static Type Resolve(string fullName)
    {
        if (Types.TryGetValue(fullName, out var type))
        {
            return type;
        }

        throw new PermanentMessagingException($"Unknown integration contract '{fullName}'.");
    }

    public static string KafkaTopicFor(Type type) => type.Namespace switch
    {
        "FoodFlow.Contracts.Payments" => KafkaTopics.Payments,
        _ => KafkaTopics.Orders
    };

    public static bool IsAnalyticsEvent(Type type) =>
        type == typeof(OrderCreated)
        || type == typeof(OrderConfirmed)
        || type == typeof(OrderCancelled)
        || type == typeof(PaymentCompleted)
        || type == typeof(PaymentFailed)
        || type == typeof(InventoryReserved)
        || type == typeof(InventoryReservationFailed)
        || type == typeof(ProductCreated);
}
