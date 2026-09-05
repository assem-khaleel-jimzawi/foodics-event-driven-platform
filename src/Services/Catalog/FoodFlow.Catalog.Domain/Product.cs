namespace FoodFlow.Catalog.Domain;

public sealed class Product
{
    private Product()
    {
        Name = string.Empty;
        Category = string.Empty;
        Currency = string.Empty;
    }

    private Product(
        Guid id,
        string name,
        string category,
        decimal price,
        string currency,
        bool isAvailable,
        DateTimeOffset createdAtUtc)
    {
        Id = id;
        Name = name;
        Category = category;
        Price = price;
        Currency = currency;
        IsAvailable = isAvailable;
        CreatedAtUtc = createdAtUtc;
    }

    public Guid Id { get; private set; }
    public string Name { get; private set; }
    public string Category { get; private set; }
    public decimal Price { get; private set; }
    public string Currency { get; private set; }
    public bool IsAvailable { get; private set; }
    public DateTimeOffset CreatedAtUtc { get; private set; }
    public DateTimeOffset? UpdatedAtUtc { get; private set; }

    public static Product Create(
        string name,
        string category,
        decimal price,
        string currency,
        bool isAvailable = true,
        Guid? id = null,
        DateTimeOffset? createdAtUtc = null)
    {
        return new Product(
            id ?? Guid.NewGuid(),
            NormalizeName(name),
            NormalizeCategory(category),
            NormalizePrice(price),
            NormalizeCurrency(currency),
            isAvailable,
            createdAtUtc ?? DateTimeOffset.UtcNow);
    }

    public void ChangePrice(decimal price)
    {
        var normalized = NormalizePrice(price);
        if (normalized == Price)
        {
            return;
        }

        Price = normalized;
        UpdatedAtUtc = DateTimeOffset.UtcNow;
    }

    public void ChangeAvailability(bool isAvailable)
    {
        if (IsAvailable == isAvailable)
        {
            return;
        }

        IsAvailable = isAvailable;
        UpdatedAtUtc = DateTimeOffset.UtcNow;
    }

    private static string NormalizeName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new DomainException("invalid_product_name", "Product name is required.");
        }

        return name.Trim();
    }

    private static string NormalizeCategory(string category)
    {
        if (string.IsNullOrWhiteSpace(category))
        {
            throw new DomainException("invalid_category", "Category is required.");
        }

        return category.Trim();
    }

    private static decimal NormalizePrice(decimal price)
    {
        if (price < 0)
        {
            throw new DomainException("invalid_price", "Price cannot be negative.");
        }

        return decimal.Round(price, 2, MidpointRounding.AwayFromZero);
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
