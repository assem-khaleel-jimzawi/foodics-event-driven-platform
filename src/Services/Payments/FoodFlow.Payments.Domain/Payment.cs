namespace FoodFlow.Payments.Domain;

public sealed class DomainException : Exception
{
    public string Code { get; }

    public DomainException(string code, string message) : base(message) => Code = code;
}

public enum PaymentStatus
{
    Completed = 0,
    Failed = 1
}

public sealed class Payment
{
    private Payment()
    {
        Currency = string.Empty;
    }

    private Payment(Guid id, Guid orderId, decimal amount, string currency, PaymentStatus status, string? failureReason)
    {
        Id = id;
        OrderId = orderId;
        Amount = amount;
        Currency = currency;
        Status = status;
        FailureReason = failureReason;
        TransactionId = status == PaymentStatus.Completed ? Guid.NewGuid() : null;
        CreatedAtUtc = DateTimeOffset.UtcNow;
    }

    public Guid Id { get; private set; }
    public Guid OrderId { get; private set; }
    public decimal Amount { get; private set; }
    public string Currency { get; private set; }
    public PaymentStatus Status { get; private set; }
    public Guid? TransactionId { get; private set; }
    public string? FailureReason { get; private set; }
    public DateTimeOffset CreatedAtUtc { get; private set; }

    public static Payment Completed(Guid orderId, decimal amount, string currency) =>
        new(Guid.NewGuid(), orderId, amount, currency, PaymentStatus.Completed, null);

    public static Payment Failed(Guid orderId, decimal amount, string currency, string reason) =>
        new(Guid.NewGuid(), orderId, amount, currency, PaymentStatus.Failed, reason);
}
