using FoodFlow.Orders.Domain;

namespace FoodFlow.Orders.Application;

public sealed class OrderService(IOrderStore store)
{
    public async Task<OrderResponse> CreateAsync(CreateOrderRequest request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var items = request.Items ?? [];
        var order = Order.Create(
            request.RestaurantId,
            request.CustomerId,
            request.Currency,
            items.Select(item => (item.ProductId, item.ProductName, item.Quantity, item.UnitPrice)).ToList());

        await store.AddAsync(order, cancellationToken);
        await store.SaveChangesAsync(cancellationToken);

        return Map(order);
    }

    public async Task<OrderResponse?> GetAsync(Guid id, CancellationToken cancellationToken)
    {
        var order = await store.GetByIdAsync(id, cancellationToken);
        return order is null ? null : Map(order);
    }

    private static OrderResponse Map(Order order) =>
        new(
            order.Id,
            order.RestaurantId,
            order.CustomerId,
            order.Status.ToString(),
            order.TotalAmount,
            order.Currency,
            order.CreatedAtUtc,
            order.UpdatedAtUtc,
            order.Items.Select(item => new OrderItemResponse(
                item.ProductId,
                item.ProductName,
                item.Quantity,
                item.UnitPrice,
                item.LineTotal)).ToList());
}
