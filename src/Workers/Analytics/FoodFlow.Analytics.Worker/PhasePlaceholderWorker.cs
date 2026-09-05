namespace FoodFlow.Analytics.Worker;

public sealed class PhasePlaceholderWorker(ILogger<PhasePlaceholderWorker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation(
            "Analytics worker is a Phase 1 host skeleton. It will consume Kafka topics such as foodflow.orders and foodflow.payments in Phase 2.");

        try
        {
            await Task.Delay(Timeout.InfiniteTimeSpan, stoppingToken);
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            logger.LogInformation("Analytics worker is stopping.");
        }
    }
}
