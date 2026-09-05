namespace FoodFlow.Notifications.Worker;

public sealed class PhasePlaceholderWorker(ILogger<PhasePlaceholderWorker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation(
            "Notifications worker is a Phase 1 host skeleton. It will consume OrderConfirmed, OrderCancelled, PaymentCompleted, and PaymentFailed in Phase 2.");

        try
        {
            await Task.Delay(Timeout.InfiniteTimeSpan, stoppingToken);
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            logger.LogInformation("Notifications worker is stopping.");
        }
    }
}
