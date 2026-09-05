using Confluent.Kafka;
using Confluent.Kafka.Admin;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace FoodFlow.Messaging;

/// <summary>
/// Creates analytics topics before producers and consumers race. Subscribe on a missing topic
/// does not auto-create by default; the consumer would otherwise wait for a 5-minute metadata refresh.
/// </summary>
public sealed class KafkaTopicProvisioner(
    IOptions<KafkaOptions> options,
    ILogger<KafkaTopicProvisioner> logger) : IHostedService
{
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        var kafka = options.Value;
        var topics = new[] { kafka.OrdersTopic, kafka.PaymentsTopic };
        using var admin = new AdminClientBuilder(new AdminClientConfig
        {
            BootstrapServers = kafka.BootstrapServers
        }).Build();

        for (var attempt = 1; attempt <= 8; attempt++)
        {
            try
            {
                await admin.CreateTopicsAsync(topics.Select(name => new TopicSpecification
                {
                    Name = name,
                    NumPartitions = 3,
                    ReplicationFactor = 1
                }));
                logger.LogInformation("Ensured Kafka topics {Topics}", string.Join(", ", topics));
                return;
            }
            catch (CreateTopicsException exception) when (exception.Results.All(result =>
                result.Error.Code == ErrorCode.TopicAlreadyExists))
            {
                return;
            }
            catch (CreateTopicsException exception)
            {
                var missing = exception.Results
                    .Where(result => result.Error.Code != ErrorCode.TopicAlreadyExists)
                    .ToList();
                if (missing.Count == 0)
                {
                    return;
                }

                logger.LogWarning(exception, "Kafka topic create attempt {Attempt} had errors", attempt);
            }
            catch (Exception exception) when (attempt < 8)
            {
                logger.LogWarning(exception, "Kafka admin not ready; retry {Attempt}", attempt);
            }

            await Task.Delay(TimeSpan.FromMilliseconds(250 * attempt), cancellationToken);
        }
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
