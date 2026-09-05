using FoodFlow.Contracts.Inventory;
using FoodFlow.Messaging;
using FoodFlow.Payments.Application;
using FoodFlow.Payments.Domain;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace FoodFlow.Payments.Infrastructure;

public sealed class PaymentsDbContext(DbContextOptions<PaymentsDbContext> options) : DbContext(options), IOutboxDbContext
{
    public DbSet<Payment> Payments => Set<Payment>();
    public DbSet<OutboxMessage> OutboxMessages => Set<OutboxMessage>();
    public DbSet<InboxMessage> InboxMessages => Set<InboxMessage>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema("payments");
        modelBuilder.AddOutboxAndInbox("payments");
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(PaymentsDbContext).Assembly);
    }
}

internal sealed class PaymentConfiguration : IEntityTypeConfiguration<Payment>
{
    public void Configure(EntityTypeBuilder<Payment> builder)
    {
        builder.ToTable("payments");
        builder.HasKey(payment => payment.Id);
        builder.HasIndex(payment => payment.OrderId).IsUnique();
        builder.Property(payment => payment.Amount).HasPrecision(18, 2);
        builder.Property(payment => payment.Currency).HasMaxLength(3);
        builder.Property(payment => payment.Status).HasConversion<string>().HasMaxLength(32);
        builder.Property(payment => payment.FailureReason).HasMaxLength(500);
    }
}

public sealed class PaymentStore(PaymentsDbContext db) : IPaymentStore
{
    public Task<Payment?> GetByOrderIdAsync(Guid orderId, CancellationToken cancellationToken) =>
        db.Payments.FirstOrDefaultAsync(payment => payment.OrderId == orderId, cancellationToken);

    public Task AddAsync(Payment payment, CancellationToken cancellationToken)
    {
        db.Payments.Add(payment);
        return Task.CompletedTask;
    }

    public Task<bool> TryClaimInboxAsync(string consumer, Guid messageId, CancellationToken cancellationToken) =>
        Inbox.TryClaimAsync(db.InboxMessages, consumer, messageId, cancellationToken);

    public void Enqueue(object message, DateTimeOffset occurredAtUtc) =>
        db.AddOperational(message, occurredAtUtc);

    public Task SaveChangesAsync(CancellationToken cancellationToken) =>
        db.SaveChangesAsync(cancellationToken);
}

public sealed class InventoryReservedConsumer(PaymentService payments) : IConsumer<InventoryReserved>
{
    public Task Consume(ConsumeContext<InventoryReserved> context) =>
        payments.HandleInventoryReservedAsync(context.Message, context.CancellationToken);
}

public sealed class PaymentsDbContextFactory : IDesignTimeDbContextFactory<PaymentsDbContext>
{
    public PaymentsDbContext CreateDbContext(string[] args)
    {
        var connectionString = Environment.GetEnvironmentVariable("ConnectionStrings__PaymentsDb")
            ?? "Host=localhost;Port=5432;Database=payments;Username=foodflow;Password=foodflow";
        return new PaymentsDbContext(new DbContextOptionsBuilder<PaymentsDbContext>().UseNpgsql(connectionString).Options);
    }
}

public static class PaymentsInfrastructureExtensions
{
    public static IHostApplicationBuilder AddPaymentsInfrastructure(this IHostApplicationBuilder builder)
    {
        var connectionString = builder.Configuration.GetConnectionString("PaymentsDb")
            ?? throw new InvalidOperationException("Connection string 'PaymentsDb' is not configured.");

        builder.Services.AddDbContext<PaymentsDbContext>(options => options.UseNpgsql(connectionString));
        builder.Services.AddScoped<IPaymentStore, PaymentStore>();
        builder.Services.AddSingleton<IPaymentGateway, FakePaymentGateway>();
        builder.Services.AddScoped<PaymentService>();
        builder.Services.AddHealthChecks().AddDbContextCheck<PaymentsDbContext>("payments-db", tags: ["ready"]);
        builder.Services.AddOutboxProcessor<PaymentsDbContext>();
        return builder;
    }
}
