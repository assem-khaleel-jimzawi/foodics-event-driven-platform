namespace FoodFlow.Inventory.Domain;

public sealed class StockItem
{
    private StockItem()
    {
    }

    public StockItem(Guid productId, int onHand)
    {
        if (productId == Guid.Empty)
        {
            throw new DomainException("invalid_product_id", "Product id is required.");
        }

        if (onHand < 0)
        {
            throw new DomainException("invalid_on_hand", "On-hand quantity cannot be negative.");
        }

        ProductId = productId;
        OnHand = onHand;
    }

    public Guid ProductId { get; private set; }
    public int OnHand { get; private set; }
    public int Reserved { get; private set; }
    public int Version { get; private set; }
    public int Available => OnHand - Reserved;

    public void SetOnHand(int onHand)
    {
        if (onHand < 0)
        {
            throw new DomainException("invalid_on_hand", "On-hand quantity cannot be negative.");
        }

        if (onHand < Reserved)
        {
            throw new DomainException("invalid_on_hand", "On-hand cannot be below the reserved quantity.");
        }

        OnHand = onHand;
        Version++;
    }

    public void Reserve(int quantity)
    {
        if (quantity <= 0)
        {
            throw new DomainException("invalid_quantity", "Quantity must be greater than zero.");
        }

        if (Available < quantity)
        {
            throw new DomainException("insufficient_stock", $"Not enough stock for product {ProductId}.");
        }

        Reserved += quantity;
        Version++;
    }

    public void Release(int quantity)
    {
        if (quantity <= 0)
        {
            return;
        }

        Reserved = Math.Max(0, Reserved - quantity);
        Version++;
    }
}
