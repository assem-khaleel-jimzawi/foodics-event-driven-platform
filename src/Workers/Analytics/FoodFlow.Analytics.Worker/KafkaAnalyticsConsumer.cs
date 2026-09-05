using System.Diagnostics;
using Confluent.Kafka;
using FoodFlow.Messaging;
using Microsoft.Extensions.Options;

namespace FoodFlow.Analytics.Worker;

public sealed class KafkaAnalyticsConsumer(
    AnalyticsStore store,
    IOptions<KafkaOptions> options,
    ILogger<KafkaAnalyticsConsumer> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var kafka = options.Value;
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ConsumeUntilCanceled(kafka, stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception exception)
            {
                logger.LogWarning(exception, "Analytics Kafka consumer disconnected; retrying");
                try
                {
                    await Task.Delay(TimeSpan.FromSeconds(2), stoppingToken);
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                {
                    break;
                }
            }
        }
    }

    private Task ConsumeUntilCanceled(KafkaOptions kafka, CancellationToken stoppingToken)
    {
        var config = new ConsumerConfig
        {
            BootstrapServers = kafka.BootstrapServers,
            GroupId = kafka.AnalyticsGroupId,
            AutoOffsetReset = AutoOffsetReset.Earliest,
            EnableAutoCommit = false,
            AllowAutoCreateTopics = true,
            TopicMetadataRefreshIntervalMs = 2000,
            ClientId = "foodflow-analytics"
        };

        using var consumer = new ConsumerBuilder<string, string>(config).Build();
        consumer.Subscribe([kafka.OrdersTopic, kafka.PaymentsTopic]);
        logger.LogInformation(
            "Analytics subscribed to {OrdersTopic} and {PaymentsTopic} as {GroupId}",
            kafka.OrdersTopic,
            kafka.PaymentsTopic,
            kafka.AnalyticsGroupId);

        try
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                ConsumeResult<string, string>? result;
                try
                {
                    result = consumer.Consume(TimeSpan.FromMilliseconds(250));
                }
                catch (ConsumeException exception)
                {
                    logger.LogWarning(exception, "Kafka consume failed");
                    continue;
                }

                if (result is null)
                {
                    continue;
                }

                var typeName = ReadHeader(result.Message.Headers, "message-type");
                var traceParent = ReadHeader(result.Message.Headers, "traceparent");
                using var activity = FoodFlowTelemetry.StartFromParent(
                    "analytics.consume",
                    ActivityKind.Consumer,
                    traceParent);
                activity?.SetTag("messaging.system", "kafka");
                activity?.SetTag("messaging.destination", result.Topic);
                activity?.SetTag("messaging.kafka.partition", result.Partition.Value);
                activity?.SetTag("messaging.kafka.offset", result.Offset.Value);

                if (string.IsNullOrWhiteSpace(typeName))
                {
                    logger.LogWarning("Analytics skipped a Kafka record without message-type at {Topic}:{Offset}", result.Topic, result.Offset.Value);
                    consumer.Commit(result);
                    continue;
                }

                var started = Stopwatch.GetTimestamp();
                try
                {
                    var type = ContractTypes.Resolve(typeName);
                    var body = IntegrationSerializer.Deserialize(result.Message.Value, type);
                    store.Apply(type.Name, body);
                    consumer.Commit(result);
                    logger.LogInformation(
                        "Analytics consumed {MessageType} from {Topic} partition {Partition} offset {Offset} key {Key}",
                        type.Name,
                        result.Topic,
                        result.Partition.Value,
                        result.Offset.Value,
                        result.Message.Key);
                }
                catch (PermanentMessagingException exception)
                {
                    activity?.SetStatus(ActivityStatusCode.Error, exception.Message);
                    FoodFlowTelemetry.ConsumerFailures.Add(1, new KeyValuePair<string, object?>("component", "kafka"));
                    logger.LogError(exception, "Analytics skipped an unknown payload at {Topic}:{Offset}", result.Topic, result.Offset.Value);
                    consumer.Commit(result);
                }
                finally
                {
                    FoodFlowTelemetry.EventProcessingDuration.Record(
                        Stopwatch.GetElapsedTime(started).TotalMilliseconds,
                        new KeyValuePair<string, object?>("component", "kafka"),
                        new KeyValuePair<string, object?>("destination", result.Topic));
                }
            }
        }
        finally
        {
            consumer.Close();
        }

        return Task.CompletedTask;
    }

    private static string? ReadHeader(Headers? headers, string key)
    {
        if (headers is null || !headers.TryGetLastBytes(key, out var bytes))
        {
            return null;
        }

        return System.Text.Encoding.UTF8.GetString(bytes);
    }
}
