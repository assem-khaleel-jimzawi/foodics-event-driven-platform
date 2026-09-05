using System.Diagnostics;

namespace FoodFlow.Messaging;

public static class OutboxEnqueue
{
    public static void AddOperational(this IOutboxDbContext db, object message, DateTimeOffset? occurredAtUtc = null)
    {
        var at = occurredAtUtc ?? DateTimeOffset.UtcNow;
        var traceParent = Activity.Current?.Id;
        db.OutboxMessages.Add(OutboxMessage.ToRabbitMq(message, at, traceParent));
        if (ContractTypes.IsAnalyticsEvent(message.GetType()))
        {
            db.OutboxMessages.Add(OutboxMessage.ToKafka(message, at, traceParent));
        }
    }
}
