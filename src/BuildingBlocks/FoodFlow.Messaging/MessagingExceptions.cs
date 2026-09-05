namespace FoodFlow.Messaging;

/// <summary>
/// Transient faults are safe to retry: the broker will redeliver, the consumer must stay idempotent.
/// Examples: brief PostgreSQL unavailability, timeout.
/// </summary>
public sealed class TransientMessagingException : Exception
{
    public TransientMessagingException(string message, Exception? inner = null)
        : base(message, inner)
    {
    }
}

/// <summary>
/// Permanent faults must not be retried in a loop: invalid payload, impossible business state.
/// After MassTransit retry policy skips them, they land in the <c>_error</c> queue.
/// </summary>
public sealed class PermanentMessagingException : Exception
{
    public PermanentMessagingException(string message, Exception? inner = null)
        : base(message, inner)
    {
    }
}
