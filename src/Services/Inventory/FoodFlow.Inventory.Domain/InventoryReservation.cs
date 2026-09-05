namespace FoodFlow.Inventory.Domain;

public sealed class ReservationLine
{
    private ReservationLine()
    {
    }

    public ReservationLine(Guid productId, int quantity)
    {
        ProductId = productId;
        Quantity = quantity;
    }

    public Guid Id { get; private set; } = Guid.NewGuid();
    public Guid ProductId { get; private set; }
    public int Quantity { get; private set; }
}

public sealed class InventoryReservation
{
    private readonly List<ReservationLine> _lines = [];

    private InventoryReservation()
    {
    }

    private InventoryReservation(Guid orderId, IEnumerable<ReservationLine> lines)
    {
        OrderId = orderId;
        Status = ReservationStatus.Reserved;
        CreatedAtUtc = DateTimeOffset.UtcNow;
        _lines.AddRange(lines);
    }

    public Guid OrderId { get; private set; }
    public ReservationStatus Status { get; private set; }
    public DateTimeOffset CreatedAtUtc { get; private set; }
    public DateTimeOffset? ReleasedAtUtc { get; private set; }
    public IReadOnlyCollection<ReservationLine> Lines => _lines;

    public static InventoryReservation Create(Guid orderId, IReadOnlyList<(Guid ProductId, int Quantity)> lines)
    {
        if (orderId == Guid.Empty)
        {
            throw new DomainException("invalid_order_id", "Order id is required.");
        }

        if (lines.Count == 0)
        {
            throw new DomainException("empty_reservation", "Reservation requires items.");
        }

        return new InventoryReservation(orderId, lines.Select(line => new ReservationLine(line.ProductId, line.Quantity)));
    }

    public void Release()
    {
        if (Status == ReservationStatus.Released)
        {
            return;
        }

        Status = ReservationStatus.Released;
        ReleasedAtUtc = DateTimeOffset.UtcNow;
    }
}
