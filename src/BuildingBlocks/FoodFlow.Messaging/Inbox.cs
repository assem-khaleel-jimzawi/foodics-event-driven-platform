using Microsoft.EntityFrameworkCore;

namespace FoodFlow.Messaging;

public static class Inbox
{
    public static async Task<bool> TryClaimAsync(
        DbSet<InboxMessage> inbox,
        string consumer,
        Guid messageId,
        CancellationToken cancellationToken)
    {
        var exists = await inbox.AnyAsync(
            row => row.Consumer == consumer && row.MessageId == messageId,
            cancellationToken);

        if (exists)
        {
            return false;
        }

        inbox.Add(new InboxMessage
        {
            Consumer = consumer,
            MessageId = messageId,
            ProcessedAtUtc = DateTimeOffset.UtcNow
        });

        return true;
    }
}
