using FoodFlow.Catalog.Domain;

namespace FoodFlow.Catalog.Domain.Tests;

public sealed class ProductTests
{
    [Fact]
    public void Create_normalizes_name_category_currency_and_price()
    {
        var product = Product.Create("  Chicken Kabsa  ", " mains ", 32.456m, "sar");

        product.Name.Should().Be("Chicken Kabsa");
        product.Category.Should().Be("mains");
        product.Price.Should().Be(32.46m);
        product.Currency.Should().Be("SAR");
        product.IsAvailable.Should().BeTrue();
    }

    [Fact]
    public void Create_rejects_negative_price()
    {
        var act = () => Product.Create("Tea", "drinks", -1m, "SAR");

        act.Should().Throw<DomainException>().Where(ex => ex.Code == "invalid_price");
    }

    [Fact]
    public void ChangePrice_updates_when_the_value_changes()
    {
        var product = Product.Create("Tea", "drinks", 8m, "SAR");

        product.ChangePrice(9.5m);

        product.Price.Should().Be(9.50m);
        product.UpdatedAtUtc.Should().NotBeNull();
    }

    [Fact]
    public void ChangeAvailability_can_mark_an_item_unavailable()
    {
        var product = Product.Create("Tea", "drinks", 8m, "SAR");

        product.ChangeAvailability(false);

        product.IsAvailable.Should().BeFalse();
        product.UpdatedAtUtc.Should().NotBeNull();
    }
}
