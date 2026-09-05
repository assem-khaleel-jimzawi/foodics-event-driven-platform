using FoodFlow.Catalog.Domain;

namespace FoodFlow.Catalog.Application;

public interface IProductStore
{
    Task AddAsync(Product product, CancellationToken cancellationToken);
    Task<Product?> GetByIdAsync(Guid id, CancellationToken cancellationToken);
    Task SaveChangesAsync(CancellationToken cancellationToken);
}
