using FoodFlow.Catalog.Domain;

namespace FoodFlow.Catalog.Application;

public sealed class ProductService(IProductStore store)
{
    public async Task<ProductResponse> CreateAsync(CreateProductRequest request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var product = Product.Create(
            request.Name,
            request.Category,
            request.Price,
            request.Currency,
            request.IsAvailable);

        await store.AddAsync(product, cancellationToken);
        await store.SaveChangesAsync(cancellationToken);
        return Map(product);
    }

    public async Task<ProductResponse?> GetAsync(Guid id, CancellationToken cancellationToken)
    {
        var product = await store.GetByIdAsync(id, cancellationToken);
        return product is null ? null : Map(product);
    }

    private static ProductResponse Map(Product product) =>
        new(
            product.Id,
            product.Name,
            product.Category,
            product.Price,
            product.Currency,
            product.IsAvailable,
            product.CreatedAtUtc,
            product.UpdatedAtUtc);
}
