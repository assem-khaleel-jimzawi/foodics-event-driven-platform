namespace FoodFlow.Orders.Domain;

public sealed class OrderItem
{
    private OrderItem()
    {
        ProductName = string.Empty;
    }

    internal OrderItem(Guid productId, string productName, int quantity, decimal unitPrice)
    {
        if (productId == Guid.Empty)
        {
            throw new DomainException("invalid_product_id", "Product id is required.");
        }

        if (string.IsNullOrWhiteSpace(productName))
        {
            throw new DomainException("invalid_product_name", "Product name is required on an order line.");
        }

        if (quantity <= 0)
        {
            throw new DomainException("invalid_quantity", "Quantity must be greater than zero.");
        }

        if (unitPrice < 0)
        {
            throw new DomainException("invalid_unit_price", "Unit price cannot be negative.");
        }

        Id = Guid.NewGuid();
        ProductId = productId;
        ProductName = productName.Trim();
        Quantity = quantity;
        UnitPrice = decimal.Round(unitPrice, 2, MidpointRounding.AwayFromZero);
    }

    public Guid Id { get; private set; }
    public Guid ProductId { get; private set; }
    public string ProductName { get; private set; }
    public int Quantity { get; private set; }
    public decimal UnitPrice { get; private set; }

    public decimal LineTotal => decimal.Round(Quantity * UnitPrice, 2, MidpointRounding.AwayFromZero);
}
