namespace Hexalith.ChatBot.Workers.Mailbox;

/// <summary>Real wall-clock implementation (worker-seam default).</summary>
internal sealed class SystemMailboxIntakeClock : IMailboxIntakeClock
{
    public DateTimeOffset UtcNow => DateTimeOffset.UtcNow;
}
