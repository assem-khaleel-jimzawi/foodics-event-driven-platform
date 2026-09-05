using System.Diagnostics;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace FoodFlow.Messaging;

public sealed class OutboxProcessor<TContext>(
    IServiceScopeFactory scopeFactory,
    IOptions<OutboxOptions> options,
    ILogger<OutboxProcessor<TContext>> logger) : BackgroundService
    where TContext : DbContext, IOutboxDbContext
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var interval = options.Value.PollInterval;
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await PublishBatchAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception exception)
            {
                logger.LogError(exception, "Outbox processor loop failed");
                FoodFlowTelemetry.ConsumerFailures.Add(1, new KeyValuePair<string, object?>("component", "outbox"));
            }

            try
            {
                await Task.Delay(interval, stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
        }
    }

    private async Task PublishBatchAsync(CancellationToken cancellationToken)
    {
        await using var scope = scopeFactory.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<TContext>();
        var bus = scope.ServiceProvider.GetRequiredService<IPublishEndpoint>();
        var kafka = scope.ServiceProvider.GetService<IKafkaProducer>();

        var maxAttempts = options.Value.MaxAttempts;
        var batch = await db.OutboxMessages
            .Where(message => message.PublishedAtUtc == null && message.AttemptCount < maxAttempts)
            .OrderBy(message => message.OccurredAtUtc)
            .Take(options.Value.BatchSize)
            .ToListAsync(cancellationToken);

        if (batch.Count == 0)
        {
            return;
        }

        foreach (var message in batch)
        {
            var started = Stopwatch.GetTimestamp();
            using var activity = FoodFlowTelemetry.StartFromParent(
                "outbox.publish",
                ActivityKind.Producer,
                message.TraceParent);
            activity?.SetTag("messaging.system", message.Destination);
            activity?.SetTag("messaging.destination", message.Topic ?? message.MessageType);

            try
            {
                var type = ContractTypes.Resolve(message.MessageType);
                var body = IntegrationSerializer.Deserialize(message.Payload, type);

                if (message.Destination == MessageDestinations.Kafka)
                {
                    if (kafka is null)
                    {
                        throw new TransientMessagingException("Kafka producer is not registered.");
                    }

                    await kafka.ProduceAsync(message.Topic ?? ContractTypes.KafkaTopicFor(type), KafkaMessageKey.For(body), body, cancellationToken);
                }
                else
                {
                    await bus.Publish(body, type, cancellationToken);
                }

                message.PublishedAtUtc = DateTimeOffset.UtcNow;
                message.LastError = null;
                logger.LogInformation(
                    "Outbox published {MessageType} {OutboxId} via {Destination}",
                    message.MessageType,
                    message.Id,
                    message.Destination);
            }
            catch (PermanentMessagingException exception)
            {
                message.AttemptCount++;
                message.LastError = exception.Message;
                message.PublishedAtUtc = DateTimeOffset.UtcNow;
                activity?.SetStatus(ActivityStatusCode.Error, exception.Message);
                logger.LogError(exception, "Outbox message {OutboxId} is permanently failed and parked", message.Id);
            }
            catch (Exception exception)
            {
                message.AttemptCount++;
                message.LastError = exception.Message;
                activity?.SetStatus(ActivityStatusCode.Error, exception.Message);
                logger.LogWarning(exception, "Outbox publish attempt {Attempt} failed for {OutboxId}", message.AttemptCount, message.Id);
            }
            finally
            {
                var elapsedMs = Stopwatch.GetElapsedTime(started).TotalMilliseconds;
                FoodFlowTelemetry.EventProcessingDuration.Record(elapsedMs, new KeyValuePair<string, object?>("component", "outbox"));
            }
        }

        await db.SaveChangesAsync(cancellationToken);
    }
}
