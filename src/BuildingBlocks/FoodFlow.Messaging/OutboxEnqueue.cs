namespace FoodFlow.Messaging;

public static class OutboxEnqueue
{
    public static void AddOperational(this IOutboxDbContext db, object message, DateTimeOffset? occurredAtUtc = null)
    {
        var at = occurredAtUtc ?? DateTimeOffset.UtcNow;
        db.OutboxMessages.Add(OutboxMessage.ToRabbitMq(message, at));
        if (ContractTypes.IsAnalyticsEvent(message.GetType()))
        {
            db.OutboxMessages.Add(OutboxMessage.ToKafka(message, at));
        }
    }
}
