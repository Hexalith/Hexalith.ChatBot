namespace Hexalith.ChatBot.Workers.Mailbox;

/// <summary>Injected UTC clock for the mailbox worker — server-measured time, never client/item-supplied.</summary>
public interface IMailboxIntakeClock
{
    DateTimeOffset UtcNow { get; }
}
