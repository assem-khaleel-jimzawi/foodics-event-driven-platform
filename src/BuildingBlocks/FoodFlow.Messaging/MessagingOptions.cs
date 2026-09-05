namespace FoodFlow.Messaging;

public sealed class RabbitMqOptions
{
    public const string SectionName = "RabbitMq";

    public string Host { get; set; } = "localhost";
    public ushort Port { get; set; } = 5672;
    public string Username { get; set; } = "guest";
    public string Password { get; set; } = "guest";
    public string VirtualHost { get; set; } = "/";
}

public sealed class KafkaOptions
{
    public const string SectionName = "Kafka";

    public string BootstrapServers { get; set; } = "localhost:9092";
    public string OrdersTopic { get; set; } = KafkaTopics.Orders;
    public string PaymentsTopic { get; set; } = KafkaTopics.Payments;
    public string AnalyticsGroupId { get; set; } = "foodflow-analytics";
}

public sealed class OutboxOptions
{
    public const string SectionName = "Outbox";

    public TimeSpan PollInterval { get; set; } = TimeSpan.FromMilliseconds(400);
    public int BatchSize { get; set; } = 20;
    public int MaxAttempts { get; set; } = 25;
}
