using FoodFlow.Catalog.Application;
using FoodFlow.Catalog.Domain;
using Microsoft.EntityFrameworkCore;

namespace FoodFlow.Catalog.Infrastructure;

public sealed class ProductStore(CatalogDbContext db) : IProductStore
{
    public async Task AddAsync(Product product, CancellationToken cancellationToken)
    {
        await db.Products.AddAsync(product, cancellationToken);
    }

    public Task<Product?> GetByIdAsync(Guid id, CancellationToken cancellationToken) =>
        db.Products.AsNoTracking().FirstOrDefaultAsync(product => product.Id == id, cancellationToken);

    public Task SaveChangesAsync(CancellationToken cancellationToken) =>
        db.SaveChangesAsync(cancellationToken);
}
