using FoodFlow.Contracts.Inventory;
using FoodFlow.Inventory.Application;
using MassTransit;

namespace FoodFlow.Inventory.Infrastructure;

public sealed class ReserveInventoryConsumer(InventoryService inventory) : IConsumer<ReserveInventory>
{
    public Task Consume(ConsumeContext<ReserveInventory> context) =>
        inventory.HandleReserveAsync(context.Message, context.CancellationToken);
}

public sealed class ReleaseInventoryConsumer(InventoryService inventory) : IConsumer<ReleaseInventory>
{
    public Task Consume(ConsumeContext<ReleaseInventory> context) =>
        inventory.HandleReleaseAsync(context.Message, context.CancellationToken);
}
