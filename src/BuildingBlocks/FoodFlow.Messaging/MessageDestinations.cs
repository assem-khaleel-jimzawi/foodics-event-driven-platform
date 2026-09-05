namespace FoodFlow.Messaging;

public static class MessageDestinations
{
    public const string RabbitMq = "rabbitmq";
    public const string Kafka = "kafka";
}

public static class KafkaTopics
{
    public const string Orders = "foodflow.orders";
    public const string Payments = "foodflow.payments";
}
