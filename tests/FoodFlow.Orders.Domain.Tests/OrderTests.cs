using FoodFlow.Orders.Domain;

namespace FoodFlow.Orders.Domain.Tests;

public sealed class OrderTests
{
    private static readonly Guid RestaurantId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
    private static readonly Guid CustomerId = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");
    private static readonly Guid BurgerId = Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccccc");
    private static readonly Guid FriesId = Guid.Parse("dddddddd-dddd-dddd-dddd-dddddddddddd");

    [Fact]
    public void Create_calculates_total_from_line_items_and_ignores_client_supplied_totals()
    {
        var order = Order.Create(
            RestaurantId,
            CustomerId,
            "sar",
            [
                (BurgerId, "Burger", 2, 18.50m),
                (FriesId, "Fries", 1, 8.00m)
            ]);

        order.Status.Should().Be(OrderStatus.Created);
        order.Currency.Should().Be("SAR");
        order.Items.Should().HaveCount(2);
        order.TotalAmount.Should().Be(45.00m);
        order.CreatedAtUtc.Offset.Should().Be(TimeSpan.Zero);
    }

    [Fact]
    public void Create_rejects_empty_items()
    {
        var act = () => Order.Create(RestaurantId, CustomerId, "SAR", []);

        act.Should().Throw<DomainException>().Where(ex => ex.Code == "empty_order");
    }

    [Fact]
    public void Create_rejects_non_positive_quantity()
    {
        var act = () => Order.Create(
            RestaurantId,
            CustomerId,
            "SAR",
            [(BurgerId, "Burger", 0, 10m)]);

        act.Should().Throw<DomainException>().Where(ex => ex.Code == "invalid_quantity");
    }

    [Fact]
    public void Create_rejects_invalid_currency()
    {
        var act = () => Order.Create(
            RestaurantId,
            CustomerId,
            "RIYAL",
            [(BurgerId, "Burger", 1, 10m)]);

        act.Should().Throw<DomainException>().Where(ex => ex.Code == "invalid_currency");
    }

    [Fact]
    public void Cancel_moves_created_order_to_cancelled()
    {
        var order = Order.Create(
            RestaurantId,
            CustomerId,
            "SAR",
            [(BurgerId, "Burger", 1, 10m)]);

        order.Cancel();

        order.Status.Should().Be(OrderStatus.Cancelled);
        order.UpdatedAtUtc.Should().NotBeNull();
    }

    [Fact]
    public void Cancel_is_rejected_when_already_cancelled()
    {
        var order = Order.Create(
            RestaurantId,
            CustomerId,
            "SAR",
            [(BurgerId, "Burger", 1, 10m)]);
        order.Cancel();

        var act = () => order.Cancel();

        act.Should().Throw<DomainException>().Where(ex => ex.Code == "order_already_cancelled");
    }

    [Fact]
    public void Confirm_then_cancel_is_rejected_until_compensation_exists()
    {
        var order = Order.Create(
            RestaurantId,
            CustomerId,
            "SAR",
            [(BurgerId, "Burger", 1, 10m)]);
        order.Confirm();

        var act = () => order.Cancel();

        order.Status.Should().Be(OrderStatus.Confirmed);
        act.Should().Throw<DomainException>().Where(ex => ex.Code == "order_cannot_be_cancelled");
    }
}
