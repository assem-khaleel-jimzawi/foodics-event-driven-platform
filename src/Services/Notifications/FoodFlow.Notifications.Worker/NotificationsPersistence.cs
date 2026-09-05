using FoodFlow.Messaging;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace FoodFlow.Notifications.Worker;

public sealed class NotificationRecord
{
    public Guid Id { get; set; }
    public Guid OrderId { get; set; }
    public string Type { get; set; } = string.Empty;
    public string Channel { get; set; } = "email";
    public string Body { get; set; } = string.Empty;
    public DateTimeOffset CreatedAtUtc { get; set; }
}

public sealed class NotificationsDbContext(DbContextOptions<NotificationsDbContext> options)
    : DbContext(options)
{
    public DbSet<NotificationRecord> Notifications => Set<NotificationRecord>();
    public DbSet<InboxMessage> InboxMessages => Set<InboxMessage>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema("notifications");
        modelBuilder.Entity<NotificationRecord>(builder =>
        {
            builder.ToTable("notifications");
            builder.HasKey(notification => notification.Id);
            builder.Property(notification => notification.Type).HasMaxLength(64).IsRequired();
            builder.Property(notification => notification.Channel).HasMaxLength(32).IsRequired();
            builder.Property(notification => notification.Body).HasMaxLength(2000).IsRequired();
            builder.HasIndex(notification => notification.OrderId);
        });

        modelBuilder.Entity<InboxMessage>(builder =>
        {
            builder.ToTable("inbox_messages");
            builder.HasKey(message => new { message.Consumer, message.MessageId });
            builder.Property(message => message.Consumer).HasMaxLength(128);
        });
    }
}

public sealed class NotificationsDbContextFactory : IDesignTimeDbContextFactory<NotificationsDbContext>
{
    public NotificationsDbContext CreateDbContext(string[] args)
    {
        var connectionString = Environment.GetEnvironmentVariable("ConnectionStrings__NotificationsDb")
            ?? "Host=localhost;Port=5432;Database=notifications;Username=foodflow;Password=foodflow";
        return new NotificationsDbContext(
            new DbContextOptionsBuilder<NotificationsDbContext>().UseNpgsql(connectionString).Options);
    }
}
