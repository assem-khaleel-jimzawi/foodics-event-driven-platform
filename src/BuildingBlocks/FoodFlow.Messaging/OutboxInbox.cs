namespace FoodFlow.Messaging;

/// <summary>
/// Outbox row stored in the *local* service database. Each service has its own table.
/// The CLR type is shared as a technical primitive, not as a shared domain entity.
/// </summary>
public sealed class OutboxMessage
{
    public Guid Id { get; set; }
    public string MessageType { get; set; } = string.Empty;
    public string Payload { get; set; } = string.Empty;
    public string Destination { get; set; } = MessageDestinations.RabbitMq;
    public string? Topic { get; set; }
    public string? TraceParent { get; set; }
    public DateTimeOffset OccurredAtUtc { get; set; }
    public DateTimeOffset? PublishedAtUtc { get; set; }
    public int AttemptCount { get; set; }
    public string? LastError { get; set; }

    public static OutboxMessage ToRabbitMq(object message, DateTimeOffset occurredAtUtc, string? traceParent = null)
    {
        var type = message.GetType();
        return new OutboxMessage
        {
            Id = Guid.NewGuid(),
            MessageType = type.FullName ?? type.Name,
            Payload = IntegrationSerializer.Serialize(message, type),
            Destination = MessageDestinations.RabbitMq,
            OccurredAtUtc = occurredAtUtc,
            TraceParent = traceParent
        };
    }

    public static OutboxMessage ToKafka(object message, DateTimeOffset occurredAtUtc, string? traceParent = null)
    {
        var type = message.GetType();
        return new OutboxMessage
        {
            Id = Guid.NewGuid(),
            MessageType = type.FullName ?? type.Name,
            Payload = IntegrationSerializer.Serialize(message, type),
            Destination = MessageDestinations.Kafka,
            Topic = ContractTypes.KafkaTopicFor(type),
            OccurredAtUtc = occurredAtUtc,
            TraceParent = traceParent
        };
    }
}

public sealed class InboxMessage
{
    public string Consumer { get; set; } = string.Empty;
    public Guid MessageId { get; set; }
    public DateTimeOffset ProcessedAtUtc { get; set; }
}

public interface IOutboxDbContext
{
    Microsoft.EntityFrameworkCore.DbSet<OutboxMessage> OutboxMessages { get; }
    Microsoft.EntityFrameworkCore.DbSet<InboxMessage> InboxMessages { get; }
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
