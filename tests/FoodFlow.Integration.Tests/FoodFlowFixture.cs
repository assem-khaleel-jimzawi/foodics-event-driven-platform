using System.Net.Http.Json;
using System.Text.Json;
using FoodFlow.Contracts;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Testcontainers.Kafka;
using Testcontainers.PostgreSql;
using Testcontainers.RabbitMq;

namespace FoodFlow.Integration.Tests;

[CollectionDefinition("foodflow")]
public sealed class FoodFlowCollection : ICollectionFixture<FoodFlowFixture>
{
}

public sealed class FoodFlowFixture : IAsyncLifetime
{
    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder("postgres:16.15-alpine")
        .WithUsername("foodflow")
        .WithPassword("foodflow")
        .WithDatabase("postgres")
        .Build();

    private readonly RabbitMqContainer _rabbit = new RabbitMqBuilder("rabbitmq:3.13.7-management-alpine")
        .WithUsername("foodflow")
        .WithPassword("foodflow")
        .WithPortBinding(15672, true)
        .Build();

    private readonly KafkaContainer _kafka = new KafkaBuilder("confluentinc/cp-kafka:7.8.0").Build();

    private WebApplicationFactory<FoodFlow.Orders.Api.AssemblyMarker>? _orders;
    private WebApplicationFactory<FoodFlow.Inventory.Api.AssemblyMarker>? _inventory;
    private WebApplicationFactory<FoodFlow.Payments.Api.AssemblyMarker>? _payments;
    private WebApplicationFactory<FoodFlow.Notifications.Worker.AssemblyMarker>? _notifications;
    private WebApplicationFactory<FoodFlow.Analytics.Worker.AssemblyMarker>? _analytics;

    public HttpClient Orders { get; private set; } = null!;
    public HttpClient Inventory { get; private set; } = null!;

    public bool InventoryIsRunning => _inventory is not null;
    public HttpClient Payments { get; private set; } = null!;
    public HttpClient Notifications { get; private set; } = null!;
    public HttpClient Analytics { get; private set; } = null!;

    public string RabbitHost => _rabbit.Hostname;
    public int RabbitPort => _rabbit.GetMappedPublicPort(5672);
    public int RabbitManagementPort => _rabbit.GetMappedPublicPort(15672);
    public string KafkaBootstrap => _kafka.GetBootstrapAddress();

    public JsonSerializerOptions Json { get; } = new()
    {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public async Task<JsonElement> ReadJsonAsync(HttpResponseMessage response)
    {
        response.EnsureSuccessStatusCode();
        return JsonSerializer.Deserialize<JsonElement>(await response.Content.ReadAsStringAsync(), Json);
    }

    public async Task InitializeAsync()
    {
        await Task.WhenAll(_postgres.StartAsync(), _rabbit.StartAsync(), _kafka.StartAsync());
        await EnsureDatabase("orders");
        await EnsureDatabase("inventory");
        await EnsureDatabase("payments");
        await EnsureDatabase("notifications");

        _payments = CreateFactory<FoodFlow.Payments.Api.AssemblyMarker>(
            "ConnectionStrings:PaymentsDb", Database("payments"));
        _inventory = CreateFactory<FoodFlow.Inventory.Api.AssemblyMarker>(
            "ConnectionStrings:InventoryDb", Database("inventory"));
        _orders = CreateFactory<FoodFlow.Orders.Api.AssemblyMarker>(
            "ConnectionStrings:OrdersDb", Database("orders"));
        _notifications = CreateFactory<FoodFlow.Notifications.Worker.AssemblyMarker>(
            "ConnectionStrings:NotificationsDb", Database("notifications"));
        _analytics = CreateFactory<FoodFlow.Analytics.Worker.AssemblyMarker>(null, null);

        Payments = _payments.CreateClient();
        Inventory = _inventory.CreateClient();
        Orders = _orders.CreateClient();
        Notifications = _notifications.CreateClient();
        Analytics = _analytics.CreateClient();
    }

    public async Task RestartInventoryAsync()
    {
        if (_inventory is not null)
        {
            await _inventory.DisposeAsync();
        }

        _inventory = CreateFactory<FoodFlow.Inventory.Api.AssemblyMarker>(
            "ConnectionStrings:InventoryDb", Database("inventory"));
        Inventory = _inventory.CreateClient();
    }

    public async Task StopInventoryAsync()
    {
        if (_inventory is not null)
        {
            await _inventory.DisposeAsync();
            _inventory = null;
            Inventory = null!;
        }
    }

    public async Task DisposeAsync()
    {
        if (_orders is not null)
        {
            await _orders.DisposeAsync();
        }

        if (_inventory is not null)
        {
            await _inventory.DisposeAsync();
        }

        if (_payments is not null)
        {
            await _payments.DisposeAsync();
        }

        if (_notifications is not null)
        {
            await _notifications.DisposeAsync();
        }

        if (_analytics is not null)
        {
            await _analytics.DisposeAsync();
        }

        await Task.WhenAll(_postgres.DisposeAsync().AsTask(), _rabbit.DisposeAsync().AsTask(), _kafka.DisposeAsync().AsTask());
    }

    private WebApplicationFactory<TMarker> CreateFactory<TMarker>(string? connectionKey, string? connectionString)
        where TMarker : class
    {
        return new WebApplicationFactory<TMarker>().WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment("Development");
            if (connectionKey is not null && connectionString is not null)
            {
                builder.UseSetting(connectionKey, connectionString);
            }

            builder.UseSetting("Logging:LogLevel:Default", "Warning");
            builder.UseSetting("Logging:LogLevel:Microsoft", "Warning");
            builder.UseSetting("Logging:LogLevel:Microsoft.EntityFrameworkCore", "Warning");
            builder.UseSetting("Logging:LogLevel:FoodFlow", "Information");
            builder.UseSetting("RabbitMq:Host", RabbitHost);
            builder.UseSetting("RabbitMq:Port", RabbitPort.ToString());
            builder.UseSetting("RabbitMq:Username", "foodflow");
            builder.UseSetting("RabbitMq:Password", "foodflow");
            builder.UseSetting("Kafka:BootstrapServers", KafkaBootstrap);
            builder.UseSetting("Outbox:PollInterval", "00:00:00.200");
        });
    }

    private string Database(string name)
    {
        var cs = _postgres.GetConnectionString();
        return cs.Replace("Database=postgres", $"Database={name}", StringComparison.OrdinalIgnoreCase);
    }

    private async Task EnsureDatabase(string name)
    {
        var result = await _postgres.ExecAsync(["psql", "-U", "foodflow", "-d", "postgres", "-c", $"CREATE DATABASE {name};"]);
        if (result.ExitCode != 0 && result.Stderr.Contains("already exists", StringComparison.OrdinalIgnoreCase) == false
            && result.Stdout.Contains("already exists", StringComparison.OrdinalIgnoreCase) == false)
        {
            throw new InvalidOperationException($"Could not create database {name}: {result.Stderr}");
        }
    }

    public static object OrderBody(Guid customerId, Guid productId, int quantity = 1, decimal unitPrice = 32.50m) => new
    {
        restaurantId = DemoScenarios.DefaultRestaurantId,
        customerId,
        currency = "SAR",
        items = new[]
        {
            new
            {
                productId,
                productName = "Chicken Kabsa",
                quantity,
                unitPrice
            }
        }
    };

    public async Task<JsonElement> PostOrderAsync(object body)
    {
        var response = await Orders.PostAsJsonAsync("/api/orders", body);
        response.EnsureSuccessStatusCode();
        return JsonSerializer.Deserialize<JsonElement>(await response.Content.ReadAsStringAsync(), Json);
    }

    public async Task<JsonElement> GetOrderAsync(Guid id)
    {
        var response = await Orders.GetAsync($"/api/orders/{id}");
        response.EnsureSuccessStatusCode();
        return JsonSerializer.Deserialize<JsonElement>(await response.Content.ReadAsStringAsync(), Json);
    }

    public async Task<JsonElement> WaitForOrderStatusAsync(Guid id, string status, TimeSpan? timeout = null)
    {
        return await WaitUntilAsync(
            async () => await GetOrderAsync(id),
            order => string.Equals(order.GetProperty("status").GetString(), status, StringComparison.OrdinalIgnoreCase),
            timeout ?? TimeSpan.FromSeconds(40),
            $"order {id} to become {status}");
    }

    public static async Task<T> WaitUntilAsync<T>(
        Func<Task<T>> probe,
        Func<T, bool> ready,
        TimeSpan timeout,
        string description)
    {
        var deadline = DateTime.UtcNow + timeout;
        T? last = default;
        Exception? lastError = null;
        while (DateTime.UtcNow < deadline)
        {
            try
            {
                last = await probe();
                if (ready(last))
                {
                    return last;
                }
            }
            catch (Exception exception)
            {
                lastError = exception;
            }

            await Task.Delay(200);
        }

        throw new TimeoutException($"Timed out waiting for {description}. Last={last}. Last error: {lastError}");
    }
}
