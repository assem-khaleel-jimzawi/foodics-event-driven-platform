namespace FoodFlow.Orders.Domain;

public sealed class Order
{
    private readonly List<OrderItem> _items = [];

    private Order()
    {
        Currency = string.Empty;
    }

    private Order(
        Guid id,
        Guid restaurantId,
        Guid customerId,
        string currency,
        IEnumerable<OrderItem> items,
        DateTimeOffset createdAtUtc)
    {
        Id = id;
        RestaurantId = restaurantId;
        CustomerId = customerId;
        Currency = currency;
        Status = OrderStatus.Created;
        CreatedAtUtc = createdAtUtc;
        _items.AddRange(items);
        TotalAmount = decimal.Round(_items.Sum(item => item.LineTotal), 2, MidpointRounding.AwayFromZero);

        if (TotalAmount <= 0)
        {
            throw new DomainException("invalid_total", "Order total must be greater than zero.");
        }
    }

    public Guid Id { get; private set; }
    public Guid RestaurantId { get; private set; }
    public Guid CustomerId { get; private set; }
    public OrderStatus Status { get; private set; }
    public decimal TotalAmount { get; private set; }
    public string Currency { get; private set; }
    public DateTimeOffset CreatedAtUtc { get; private set; }
    public DateTimeOffset? UpdatedAtUtc { get; private set; }
    public IReadOnlyCollection<OrderItem> Items => _items;

    public static Order Create(
        Guid restaurantId,
        Guid customerId,
        string currency,
        IReadOnlyList<(Guid ProductId, string ProductName, int Quantity, decimal UnitPrice)> items,
        DateTimeOffset? createdAtUtc = null,
        Guid? id = null)
    {
        if (restaurantId == Guid.Empty)
        {
            throw new DomainException("invalid_restaurant_id", "Restaurant id is required.");
        }

        if (customerId == Guid.Empty)
        {
            throw new DomainException("invalid_customer_id", "Customer id is required.");
        }

        var normalizedCurrency = NormalizeCurrency(currency);

        if (items is null || items.Count == 0)
        {
            throw new DomainException("empty_order", "An order must contain at least one item.");
        }

        var orderItems = items
            .Select(item => new OrderItem(item.ProductId, item.ProductName, item.Quantity, item.UnitPrice))
            .ToList();

        return new Order(
            id ?? Guid.NewGuid(),
            restaurantId,
            customerId,
            normalizedCurrency,
            orderItems,
            createdAtUtc ?? DateTimeOffset.UtcNow);
    }

    public void Confirm()
    {
        if (Status == OrderStatus.Confirmed)
        {
            return;
        }

        EnsureCreated("order_cannot_be_confirmed", "Only a created order can be confirmed.");
        Status = OrderStatus.Confirmed;
        UpdatedAtUtc = DateTimeOffset.UtcNow;
    }

    public void Cancel()
    {
        if (Status == OrderStatus.Cancelled)
        {
            return;
        }

        if (Status == OrderStatus.Confirmed)
        {
            throw new DomainException(
                "order_cannot_be_cancelled",
                "Confirmed orders are not cancelled by this compensation path.");
        }

        Status = OrderStatus.Cancelled;
        UpdatedAtUtc = DateTimeOffset.UtcNow;
    }

    private void EnsureCreated(string code, string message)
    {
        if (Status != OrderStatus.Created)
        {
            throw new DomainException(code, message);
        }
    }

    private static string NormalizeCurrency(string currency)
    {
        if (string.IsNullOrWhiteSpace(currency) || currency.Trim().Length != 3 || !currency.Trim().All(char.IsLetter))
        {
            throw new DomainException("invalid_currency", "Currency must be a 3-letter ISO code, for example SAR.");
        }

        return currency.Trim().ToUpperInvariant();
    }
}
