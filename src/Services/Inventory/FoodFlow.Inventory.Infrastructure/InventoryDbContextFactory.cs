using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace FoodFlow.Inventory.Infrastructure;

public sealed class InventoryDbContextFactory : IDesignTimeDbContextFactory<InventoryDbContext>
{
    public InventoryDbContext CreateDbContext(string[] args)
    {
        var connectionString = Environment.GetEnvironmentVariable("ConnectionStrings__InventoryDb")
            ?? "Host=localhost;Port=5432;Database=inventory;Username=foodflow;Password=foodflow";
        return new InventoryDbContext(new DbContextOptionsBuilder<InventoryDbContext>().UseNpgsql(connectionString).Options);
    }
}
