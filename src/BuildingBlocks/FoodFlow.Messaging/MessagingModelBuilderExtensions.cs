using Microsoft.EntityFrameworkCore;

namespace FoodFlow.Messaging;

public static class MessagingModelBuilderExtensions
{
    public static void AddOutboxAndInbox(this ModelBuilder modelBuilder, string schema)
    {
        modelBuilder.Entity<OutboxMessage>(builder =>
        {
            builder.ToTable("outbox_messages", schema);
            builder.HasKey(message => message.Id);
            builder.Property(message => message.MessageType).HasMaxLength(256).IsRequired();
            builder.Property(message => message.Payload).HasColumnType("jsonb").IsRequired();
            builder.Property(message => message.Destination).HasMaxLength(32).IsRequired();
            builder.Property(message => message.Topic).HasMaxLength(128);
            builder.Property(message => message.LastError).HasMaxLength(2000);
            builder.HasIndex(message => message.PublishedAtUtc);
            builder.HasIndex(message => message.OccurredAtUtc);
        });

        modelBuilder.Entity<InboxMessage>(builder =>
        {
            builder.ToTable("inbox_messages", schema);
            builder.HasKey(message => new { message.Consumer, message.MessageId });
            builder.Property(message => message.Consumer).HasMaxLength(128);
        });
    }
}
