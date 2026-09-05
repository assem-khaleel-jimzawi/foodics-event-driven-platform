using FoodFlow.Inventory.Domain;

namespace FoodFlow.Inventory.Domain.Tests;

public sealed class StockItemTests
{
    private static readonly Guid ProductId = Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccccc");

    [Fact]
    public void Reserve_reduces_available_without_changing_on_hand()
    {
        var stock = new StockItem(ProductId, 10);

        stock.Reserve(3);

        stock.OnHand.Should().Be(10);
        stock.Reserved.Should().Be(3);
        stock.Available.Should().Be(7);
    }

    [Fact]
    public void Reserve_rejects_oversell()
    {
        var stock = new StockItem(ProductId, 2);

        var act = () => stock.Reserve(3);

        act.Should().Throw<DomainException>().Where(ex => ex.Code == "insufficient_stock");
        stock.Reserved.Should().Be(0);
    }

    [Fact]
    public void Release_is_safe_to_call_after_a_reservation()
    {
        var stock = new StockItem(ProductId, 5);
        stock.Reserve(2);

        stock.Release(2);

        stock.Reserved.Should().Be(0);
        stock.Available.Should().Be(5);
    }
}
