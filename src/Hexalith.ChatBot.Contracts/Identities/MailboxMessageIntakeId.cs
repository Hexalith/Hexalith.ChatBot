namespace Hexalith.ChatBot.Contracts.Identities;

/// <summary>
/// Adapter-facing ULID identity for a mailbox message intake aggregate.
/// </summary>
public readonly record struct MailboxMessageIntakeId
{
    private MailboxMessageIntakeId(string value)
    {
        Value = value;
    }

    public string Value { get; }

    public static MailboxMessageIntakeId New()
        => new(ChatBotIdentity.NewUlid());

    public static bool TryParse(string? value, out MailboxMessageIntakeId intakeId)
    {
        if (ChatBotIdentity.TryNormalizeUlid(value, out string? normalized))
        {
            intakeId = new MailboxMessageIntakeId(normalized);
            return true;
        }

        intakeId = default;
        return false;
    }
}
