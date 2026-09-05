using FoodFlow.Inventory.Domain;
using FoodFlow.Messaging;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FoodFlow.Inventory.Infrastructure;

public sealed class InventoryDbContext(DbContextOptions<InventoryDbContext> options)
    : DbContext(options), IOutboxDbContext
{
    public DbSet<StockItem> StockItems => Set<StockItem>();
    public DbSet<InventoryReservation> Reservations => Set<InventoryReservation>();
    public DbSet<OutboxMessage> OutboxMessages => Set<OutboxMessage>();
    public DbSet<InboxMessage> InboxMessages => Set<InboxMessage>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema("inventory");
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(InventoryDbContext).Assembly);
        modelBuilder.AddOutboxAndInbox("inventory");
    }
}

internal sealed class StockItemConfiguration : IEntityTypeConfiguration<StockItem>
{
    public void Configure(EntityTypeBuilder<StockItem> builder)
    {
        builder.ToTable("stock_items", table =>
            table.HasCheckConstraint("ck_stock_not_over_reserved", "\"OnHand\" >= \"Reserved\""));
        builder.HasKey(item => item.ProductId);
        builder.Property(item => item.OnHand).IsRequired();
        builder.Property(item => item.Reserved).IsRequired();
        builder.Property(item => item.Version).IsConcurrencyToken();
        builder.Ignore(item => item.Available);
    }
}

internal sealed class InventoryReservationConfiguration : IEntityTypeConfiguration<InventoryReservation>
{
    public void Configure(EntityTypeBuilder<InventoryReservation> builder)
    {
        builder.ToTable("reservations");
        builder.HasKey(reservation => reservation.OrderId);
        builder.Property(reservation => reservation.Status).HasConversion<string>().HasMaxLength(32);
        builder.HasMany(reservation => reservation.Lines)
            .WithOne()
            .HasForeignKey("ReservationOrderId")
            .OnDelete(DeleteBehavior.Cascade);
        builder.Navigation(reservation => reservation.Lines)
            .HasField("_lines")
            .UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}

internal sealed class ReservationLineConfiguration : IEntityTypeConfiguration<ReservationLine>
{
    public void Configure(EntityTypeBuilder<ReservationLine> builder)
    {
        builder.ToTable("reservation_lines");
        builder.HasKey(line => line.Id);
        builder.Property(line => line.ProductId).IsRequired();
        builder.Property(line => line.Quantity).IsRequired();
    }
}
