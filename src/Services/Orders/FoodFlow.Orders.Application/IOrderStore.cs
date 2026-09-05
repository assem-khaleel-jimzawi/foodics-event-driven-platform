using FoodFlow.Orders.Domain;

namespace FoodFlow.Orders.Application;

public interface IOrderStore
{
    Task AddAsync(Order order, CancellationToken cancellationToken);
    Task<Order?> GetByIdAsync(Guid id, CancellationToken cancellationToken);
    Task SaveChangesAsync(CancellationToken cancellationToken);
}
