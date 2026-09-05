using System.Diagnostics;
using MassTransit;

namespace FoodFlow.Messaging;

/// <summary>
/// Records handler duration and failures. Retries still count as failures because the handler threw;
/// permanent business events (insufficient stock, declined card) do not throw and are not counted here.
/// </summary>
public sealed class TelemetryConsumeFilter<T> : IFilter<ConsumeContext<T>>
    where T : class
{
    public void Probe(ProbeContext context) => context.CreateFilterScope("foodflow-telemetry");

    public async Task Send(ConsumeContext<T> context, IPipe<ConsumeContext<T>> next)
    {
        var started = Stopwatch.GetTimestamp();
        try
        {
            await next.Send(context);
        }
        catch
        {
            FoodFlowTelemetry.ConsumerFailures.Add(
                1,
                new KeyValuePair<string, object?>("message_type", typeof(T).Name));
            throw;
        }
        finally
        {
            FoodFlowTelemetry.EventProcessingDuration.Record(
                Stopwatch.GetElapsedTime(started).TotalMilliseconds,
                new KeyValuePair<string, object?>("component", "consumer"),
                new KeyValuePair<string, object?>("message_type", typeof(T).Name));
        }
    }
}
