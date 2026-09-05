using System.Diagnostics;
using System.Diagnostics.Metrics;

namespace FoodFlow.Messaging;

/// <summary>
/// Shared ActivitySource and Meter. Prometheus names become foodflow_* from Meter "FoodFlow".
/// </summary>
public static class FoodFlowTelemetry
{
    public const string ActivitySourceName = "FoodFlow";
    public const string MeterName = "FoodFlow";

    public static readonly ActivitySource ActivitySource = new(ActivitySourceName);
    public static readonly Meter Meter = new(MeterName);

    public static readonly Counter<long> OrdersCreated = Meter.CreateCounter<long>("orders.created", description: "Orders accepted by the Orders API.");
    public static readonly Counter<long> OrdersConfirmed = Meter.CreateCounter<long>("orders.confirmed", description: "Orders confirmed after payment.");
    public static readonly Counter<long> OrdersCancelled = Meter.CreateCounter<long>("orders.cancelled", description: "Orders cancelled (inventory or payment failure).");
    public static readonly Counter<long> InventoryReservations = Meter.CreateCounter<long>("inventory.reservations", description: "Successful stock reservations.");
    public static readonly Counter<long> InventoryReservationFailures = Meter.CreateCounter<long>("inventory.reservation.failures", description: "Business reservation failures (insufficient stock).");
    public static readonly Counter<long> PaymentsCompleted = Meter.CreateCounter<long>("payments.completed", description: "Successful fake charges.");
    public static readonly Counter<long> PaymentsFailed = Meter.CreateCounter<long>("payments.failed", description: "Declined fake charges.");
    public static readonly Counter<long> Notifications = Meter.CreateCounter<long>("notifications.sent", description: "Simulated notifications recorded.");
    public static readonly Counter<long> ConsumerFailures = Meter.CreateCounter<long>("consumer.failures", description: "Consumer handler failures after retries.");
    public static readonly Histogram<double> EventProcessingDuration = Meter.CreateHistogram<double>(
        "event.processing.duration",
        unit: "ms",
        description: "Handler or outbox publish duration.");

    public static Activity? StartFromParent(string name, ActivityKind kind, string? traceParent)
    {
        return string.IsNullOrWhiteSpace(traceParent)
            ? ActivitySource.StartActivity(name, kind)
            : ActivitySource.StartActivity(name, kind, traceParent);
    }
}
