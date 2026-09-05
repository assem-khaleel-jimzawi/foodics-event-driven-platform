using FoodFlow.Contracts;
using FoodFlow.Inventory.Application;
using FoodFlow.Inventory.Domain;
using FoodFlow.Messaging;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace FoodFlow.Inventory.Infrastructure;

public static class InventoryInfrastructureExtensions
{
    public static IHostApplicationBuilder AddInventoryInfrastructure(this IHostApplicationBuilder builder)
    {
        var connectionString = builder.Configuration.GetConnectionString("InventoryDb")
            ?? throw new InvalidOperationException("Connection string 'InventoryDb' is not configured.");

        builder.Services.AddDbContext<InventoryDbContext>(options => options.UseNpgsql(connectionString));
        builder.Services.AddScoped<IInventoryStore, InventoryStore>();
        builder.Services.AddScoped<InventoryService>();
        builder.Services.AddHealthChecks()
            .AddDbContextCheck<InventoryDbContext>("inventory-db", tags: ["ready"]);
        builder.Services.AddOutboxProcessor<InventoryDbContext>();
        return builder;
    }

    public static async Task SeedDemoStockAsync(this InventoryDbContext db, CancellationToken cancellationToken = default)
    {
        async Task Ensure(Guid productId, int onHand)
        {
            if (!await db.StockItems.AnyAsync(item => item.ProductId == productId, cancellationToken))
            {
                db.StockItems.Add(new StockItem(productId, onHand));
            }
        }

        await Ensure(DemoScenarios.InStockProductId, 100);
        await Ensure(DemoScenarios.OutOfStockProductId, 0);
        await Ensure(DemoScenarios.TransientFailureProductId, 100);
        await db.SaveChangesAsync(cancellationToken);
    }
}
