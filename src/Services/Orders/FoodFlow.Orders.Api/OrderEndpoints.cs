using FoodFlow.Orders.Application;
using FoodFlow.ServiceDefaults;

namespace FoodFlow.Orders.Api;

public static class OrderEndpoints
{
    public static IEndpointRouteBuilder MapOrderEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/orders").WithTags("Orders");

        group.MapPost("/", CreateOrder)
            .WithName("CreateOrder")
            .Produces<OrderResponse>(StatusCodes.Status201Created)
            .ProducesProblem(StatusCodes.Status400BadRequest);

        group.MapGet("/{id:guid}", GetOrder)
            .WithName("GetOrder")
            .Produces<OrderResponse>()
            .ProducesProblem(StatusCodes.Status404NotFound);

        return endpoints;
    }

    private static async Task<IResult> CreateOrder(
        CreateOrderRequest request,
        OrderService orders,
        ILoggerFactory loggerFactory,
        CancellationToken cancellationToken)
    {
        var logger = loggerFactory.CreateLogger("FoodFlow.Orders.Api");
        var order = await orders.CreateAsync(request, cancellationToken);

        logger.LogInformation(
            "Order {OrderId} created for restaurant {RestaurantId} customer {CustomerId} total {TotalAmount} {Currency}",
            order.Id,
            order.RestaurantId,
            order.CustomerId,
            order.TotalAmount,
            order.Currency);

        return TypedResults.Created($"/api/orders/{order.Id}", order);
    }

    private static async Task<IResult> GetOrder(
        Guid id,
        OrderService orders,
        CancellationToken cancellationToken)
    {
        var order = await orders.GetAsync(id, cancellationToken);
        return order is null
            ? ProblemDetailsHttp.NotFound("Order not found", $"Order {id} was not found.", "orderId", id)
            : TypedResults.Ok(order);
    }
}
