namespace Hexalith.ChatBot.Contracts.Identities;

/// <summary>
/// Adapter-facing ULID identity for a governed note aggregate recorded through the command spine.
/// Mirrors <see cref="ChatBotCommandId"/>: ULID-only, never a <see cref="System.Guid"/>.
/// </summary>
public readonly record struct GovernedNoteId
{
    private GovernedNoteId(string value)
    {
        Value = value;
    }

    public string Value { get; }

    public static GovernedNoteId New()
        => new(ChatBotIdentity.NewUlid());

    public static bool TryParse(string? value, out GovernedNoteId noteId)
    {
        if (ChatBotIdentity.TryNormalizeUlid(value, out string? normalized))
        {
            noteId = new GovernedNoteId(normalized);
            return true;
        }

        noteId = default;
        return false;
    }

    public override string ToString()
        => Value;
}
