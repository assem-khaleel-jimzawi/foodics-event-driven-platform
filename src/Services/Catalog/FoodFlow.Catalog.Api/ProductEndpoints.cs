using FoodFlow.Catalog.Application;
using FoodFlow.ServiceDefaults;

namespace FoodFlow.Catalog.Api;

public static class ProductEndpoints
{
    public static IEndpointRouteBuilder MapProductEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/products").WithTags("Products");

        group.MapPost("/", CreateProduct)
            .WithName("CreateProduct")
            .Produces<ProductResponse>(StatusCodes.Status201Created)
            .ProducesProblem(StatusCodes.Status400BadRequest);

        group.MapGet("/{id:guid}", GetProduct)
            .WithName("GetProduct")
            .Produces<ProductResponse>()
            .ProducesProblem(StatusCodes.Status404NotFound);

        return endpoints;
    }

    private static async Task<IResult> CreateProduct(
        CreateProductRequest request,
        ProductService products,
        ILoggerFactory loggerFactory,
        CancellationToken cancellationToken)
    {
        var logger = loggerFactory.CreateLogger("FoodFlow.Catalog.Api");
        var product = await products.CreateAsync(request, cancellationToken);

        logger.LogInformation(
            "Product {ProductId} created in category {Category} price {Price} {Currency} available {IsAvailable}",
            product.Id,
            product.Category,
            product.Price,
            product.Currency,
            product.IsAvailable);

        return TypedResults.Created($"/api/products/{product.Id}", product);
    }

    private static async Task<IResult> GetProduct(
        Guid id,
        ProductService products,
        CancellationToken cancellationToken)
    {
        var product = await products.GetAsync(id, cancellationToken);
        return product is null
            ? ProblemDetailsHttp.NotFound("Product not found", $"Product {id} was not found.", "productId", id)
            : TypedResults.Ok(product);
    }
}
