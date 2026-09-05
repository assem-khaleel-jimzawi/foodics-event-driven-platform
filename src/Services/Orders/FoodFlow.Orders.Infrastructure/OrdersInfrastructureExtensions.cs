using FoodFlow.Orders.Application;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace FoodFlow.Orders.Infrastructure;

public static class OrdersInfrastructureExtensions
{
    public static IHostApplicationBuilder AddOrdersInfrastructure(this IHostApplicationBuilder builder)
    {
        var connectionString = builder.Configuration.GetConnectionString("OrdersDb")
            ?? throw new InvalidOperationException("Connection string 'OrdersDb' is not configured.");

        builder.Services.AddDbContext<OrdersDbContext>(options => options.UseNpgsql(connectionString));
        builder.Services.AddScoped<IOrderStore, OrderStore>();
        builder.Services.AddScoped<OrderService>();
        builder.Services.AddHealthChecks()
            .AddDbContextCheck<OrdersDbContext>("orders-db", tags: ["ready"]);

        return builder;
    }
}
