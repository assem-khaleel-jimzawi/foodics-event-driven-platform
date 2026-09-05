using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using FoodFlow.Contracts;
using FoodFlow.Contracts.Inventory;
using MassTransit;

namespace FoodFlow.Integration.Tests;

[Collection("foodflow")]
public sealed class OrderWorkflowTests(FoodFlowFixture fixture)
{
    [Fact]
    public async Task Successful_order_reserves_inventory_charges_payment_confirms_and_notifies()
    {
        var created = await fixture.PostOrderAsync(
            FoodFlowFixture.OrderBody(DemoScenarios.DefaultCustomerId, DemoScenarios.InStockProductId, 2));
        var orderId = created.GetProperty("id").GetGuid();

        var confirmed = await fixture.WaitForOrderStatusAsync(orderId, "Confirmed");
        confirmed.GetProperty("totalAmount").GetDecimal().Should().Be(65.00m);

        var payment = await FoodFlowFixture.WaitUntilAsync(
            async () =>
            {
                var response = await fixture.Payments.GetAsync($"/api/payments/by-order/{orderId}");
                response.EnsureSuccessStatusCode();
                return JsonSerializer.Deserialize<JsonElement>(await response.Content.ReadAsStringAsync(), fixture.Json);
            },
            body => body.GetProperty("status").GetString() == "Completed",
            TimeSpan.FromSeconds(20),
            "payment completed");

        payment.GetProperty("amount").GetDecimal().Should().Be(65.00m);

        var stock = await GetStockAsync(DemoScenarios.InStockProductId);
        stock.GetProperty("reserved").GetInt32().Should().BeGreaterThanOrEqualTo(2);

        var notifications = await FoodFlowFixture.WaitUntilAsync(
            async () => await fixture.ReadJsonAsync(await fixture.Notifications.GetAsync($"/api/notifications/by-order/{orderId}")),
            body => body.ValueKind == JsonValueKind.Array
                && body.GetArrayLength() > 0
                && body.EnumerateArray().Any(item => ReadString(item, "type") == "order_confirmed"),
            TimeSpan.FromSeconds(20),
            "order_confirmed notification");

        notifications.GetArrayLength().Should().BeGreaterThan(0);

        await FoodFlowFixture.WaitUntilAsync(
            async () => await fixture.ReadJsonAsync(await fixture.Analytics.GetAsync("/api/analytics/snapshot")),
            body => ReadInt(body, "ordersConfirmed") >= 1 && ReadInt(body, "paymentsCompleted") >= 1,
            TimeSpan.FromSeconds(30),
            "analytics to observe the confirmed order");
    }

    [Fact]
    public async Task Inventory_failure_cancels_the_order_without_charging()
    {
        var created = await fixture.PostOrderAsync(
            FoodFlowFixture.OrderBody(DemoScenarios.DefaultCustomerId, DemoScenarios.OutOfStockProductId));
        var orderId = created.GetProperty("id").GetGuid();

        await fixture.WaitForOrderStatusAsync(orderId, "Cancelled");

        var paymentResponse = await fixture.Payments.GetAsync($"/api/payments/by-order/{orderId}");
        paymentResponse.StatusCode.Should().Be(System.Net.HttpStatusCode.NotFound);

        await FoodFlowFixture.WaitUntilAsync(
            async () => await fixture.ReadJsonAsync(await fixture.Analytics.GetAsync("/api/analytics/snapshot")),
            body => ReadInt(body, "inventoryFailures") >= 1 && ReadInt(body, "ordersCancelled") >= 1,
            TimeSpan.FromSeconds(30),
            "analytics inventory failure");
    }

    [Fact]
    public async Task Payment_failure_releases_inventory_and_cancels_the_order()
    {
        var stockBefore = await GetStockAsync(DemoScenarios.InStockProductId);
        var reservedBefore = stockBefore.GetProperty("reserved").GetInt32();

        var created = await fixture.PostOrderAsync(
            FoodFlowFixture.OrderBody(DemoScenarios.PaymentFailCustomerId, DemoScenarios.InStockProductId));
        var orderId = created.GetProperty("id").GetGuid();

        await fixture.WaitForOrderStatusAsync(orderId, "Cancelled");

        var payment = await FoodFlowFixture.WaitUntilAsync(
            async () =>
            {
                var response = await fixture.Payments.GetAsync($"/api/payments/by-order/{orderId}");
                response.EnsureSuccessStatusCode();
                return JsonSerializer.Deserialize<JsonElement>(await response.Content.ReadAsStringAsync(), fixture.Json);
            },
            body => body.GetProperty("status").GetString() == "Failed",
            TimeSpan.FromSeconds(20),
            "failed payment");

        payment.GetProperty("failureReason").GetString().Should().NotBeNullOrWhiteSpace();

        await FoodFlowFixture.WaitUntilAsync(
            async () => await GetStockAsync(DemoScenarios.InStockProductId),
            body => body.GetProperty("reserved").GetInt32() == reservedBefore,
            TimeSpan.FromSeconds(20),
            "inventory reservation to be released");

        await FoodFlowFixture.WaitUntilAsync(
            async () => await fixture.ReadJsonAsync(await fixture.Notifications.GetAsync($"/api/notifications/by-order/{orderId}")),
            body => body.ValueKind == JsonValueKind.Array
                && body.EnumerateArray().Any(item => ReadString(item, "type") == "order_cancelled"),
            TimeSpan.FromSeconds(20),
            "cancellation notification");
    }

    [Fact]
    public async Task Duplicate_reserve_command_does_not_double_reserve()
    {
        var created = await fixture.PostOrderAsync(
            FoodFlowFixture.OrderBody(DemoScenarios.DefaultCustomerId, DemoScenarios.InStockProductId));
        var orderId = created.GetProperty("id").GetGuid();
        await fixture.WaitForOrderStatusAsync(orderId, "Confirmed");

        var reservedAfterConfirm = (await GetStockAsync(DemoScenarios.InStockProductId)).GetProperty("reserved").GetInt32();
        var command = new ReserveInventory(
            Guid.NewGuid(),
            orderId,
            DemoScenarios.DefaultRestaurantId,
            DemoScenarios.DefaultCustomerId,
            32.50m,
            "SAR",
            [new ReserveInventoryItem(DemoScenarios.InStockProductId, 1)],
            DateTimeOffset.UtcNow,
            Guid.NewGuid());

        var bus = CreateBus();
        await bus.StartAsync();
        try
        {
            await bus.Publish(command);
            await bus.Publish(command);
        }
        finally
        {
            await bus.StopAsync();
        }

        await Task.Delay(1500);
        var after = await GetStockAsync(DemoScenarios.InStockProductId);
        after.GetProperty("reserved").GetInt32().Should().Be(reservedAfterConfirm);
    }

    [Fact]
    public async Task Transient_inventory_failure_is_retried_until_it_succeeds()
    {
        var created = await fixture.PostOrderAsync(
            FoodFlowFixture.OrderBody(DemoScenarios.DefaultCustomerId, DemoScenarios.TransientFailureProductId));
        var orderId = created.GetProperty("id").GetGuid();

        await fixture.WaitForOrderStatusAsync(orderId, "Confirmed");
    }

    [Fact]
    public async Task Permanent_inventory_failure_lands_in_the_error_queue()
    {
        var command = new ReserveInventory(
            Guid.NewGuid(),
            Guid.NewGuid(),
            DemoScenarios.DefaultRestaurantId,
            DemoScenarios.DefaultCustomerId,
            10m,
            "SAR",
            [new ReserveInventoryItem(DemoScenarios.PermanentFailureProductId, 1)],
            DateTimeOffset.UtcNow,
            Guid.NewGuid());

        var bus = CreateBus();
        await bus.StartAsync();
        try
        {
            await bus.Publish(command);
        }
        finally
        {
            await bus.StopAsync();
        }

        await FoodFlowFixture.WaitUntilAsync(
            async () => await GetErrorQueueReadyCountAsync("reserve-inventory_error"),
            count => count >= 1,
            TimeSpan.FromSeconds(20),
            "reserve-inventory_error to receive the poison message");
    }

    [Fact]
    public async Task Inventory_restart_does_not_drop_an_in_flight_order()
    {
        await fixture.StopInventoryAsync();
        try
        {
            var created = await fixture.PostOrderAsync(
                FoodFlowFixture.OrderBody(DemoScenarios.DefaultCustomerId, DemoScenarios.InStockProductId));
            var orderId = created.GetProperty("id").GetGuid();

            await Task.Delay(1500);
            var pending = await fixture.GetOrderAsync(orderId);
            pending.GetProperty("status").GetString().Should().Be("Created");

            await fixture.RestartInventoryAsync();
            await fixture.WaitForOrderStatusAsync(orderId, "Confirmed");
        }
        finally
        {
            if (!fixture.InventoryIsRunning)
            {
                await fixture.RestartInventoryAsync();
            }
        }
    }

    private async Task<JsonElement> GetStockAsync(Guid productId)
    {
        var response = await fixture.Inventory.GetAsync($"/api/inventory/stock/{productId}");
        return await fixture.ReadJsonAsync(response);
    }

    private static int ReadInt(JsonElement element, string name)
    {
        foreach (var property in element.EnumerateObject())
        {
            if (string.Equals(property.Name, name, StringComparison.OrdinalIgnoreCase))
            {
                return property.Value.GetInt32();
            }
        }

        return 0;
    }

    private static string? ReadString(JsonElement element, string name)
    {
        foreach (var property in element.EnumerateObject())
        {
            if (string.Equals(property.Name, name, StringComparison.OrdinalIgnoreCase))
            {
                return property.Value.GetString();
            }
        }

        return null;
    }

    private IBusControl CreateBus()
    {
        return Bus.Factory.CreateUsingRabbitMq(cfg =>
        {
            cfg.Host(fixture.RabbitHost, (ushort)fixture.RabbitPort, "/", host =>
            {
                host.Username("foodflow");
                host.Password("foodflow");
            });
        });
    }

    private async Task<int> GetErrorQueueReadyCountAsync(string queue)
    {
        using var client = new HttpClient
        {
            BaseAddress = new Uri($"http://{fixture.RabbitHost}:{fixture.RabbitManagementPort}/")
        };
        var token = Convert.ToBase64String(Encoding.UTF8.GetBytes("foodflow:foodflow"));
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", token);
        var encoded = Uri.EscapeDataString(queue);
        var response = await client.GetAsync($"api/queues/%2F/{encoded}");
        if (!response.IsSuccessStatusCode)
        {
            return 0;
        }

        var body = JsonSerializer.Deserialize<JsonElement>(await response.Content.ReadAsStringAsync());
        return body.TryGetProperty("messages", out var messages) ? messages.GetInt32() : 0;
    }
}
