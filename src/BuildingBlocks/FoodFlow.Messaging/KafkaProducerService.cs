using Confluent.Kafka;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace FoodFlow.Messaging;

public interface IKafkaProducer
{
    Task ProduceAsync(string topic, string key, object value, CancellationToken cancellationToken);
}

public sealed class KafkaProducerService : IKafkaProducer, IDisposable
{
    private readonly IProducer<string, string> _producer;
    private readonly ILogger<KafkaProducerService> _logger;

    public KafkaProducerService(IOptions<KafkaOptions> options, ILogger<KafkaProducerService> logger)
    {
        _logger = logger;
        var config = new ProducerConfig
        {
            BootstrapServers = options.Value.BootstrapServers,
            Acks = Acks.All,
            EnableIdempotence = true,
            ClientId = "foodflow-outbox"
        };
        _producer = new ProducerBuilder<string, string>(config).Build();
    }

    public async Task ProduceAsync(string topic, string key, object value, CancellationToken cancellationToken)
    {
        var json = IntegrationSerializer.Serialize(value, value.GetType());
        var result = await _producer.ProduceAsync(
            topic,
            new Message<string, string>
            {
                Key = key,
                Value = json,
                Headers =
                [
                    new Header("message-type", System.Text.Encoding.UTF8.GetBytes(value.GetType().FullName ?? value.GetType().Name))
                ]
            },
            cancellationToken);

        _logger.LogInformation(
            "Kafka produced {MessageType} to {Topic} partition {Partition} offset {Offset}",
            value.GetType().Name,
            result.Topic,
            result.Partition.Value,
            result.Offset.Value);
    }

    public void Dispose() => _producer.Dispose();
}
