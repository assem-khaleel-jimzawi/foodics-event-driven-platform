using FoodFlow.Catalog.Application;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace FoodFlow.Catalog.Infrastructure;

public static class CatalogInfrastructureExtensions
{
    public static IHostApplicationBuilder AddCatalogInfrastructure(this IHostApplicationBuilder builder)
    {
        var connectionString = builder.Configuration.GetConnectionString("CatalogDb")
            ?? throw new InvalidOperationException("Connection string 'CatalogDb' is not configured.");

        builder.Services.AddDbContext<CatalogDbContext>(options => options.UseNpgsql(connectionString));
        builder.Services.AddScoped<IProductStore, ProductStore>();
        builder.Services.AddScoped<ProductService>();
        builder.Services.AddHealthChecks()
            .AddDbContextCheck<CatalogDbContext>("catalog-db", tags: ["ready"]);

        return builder;
    }
}
