namespace Hexalith.ChatBot.Contracts.Identities;

/// <summary>
/// ChatBot-owned ULID identity for one mailbox participant resolution run.
/// </summary>
public readonly record struct ParticipantResolutionId
{
    private ParticipantResolutionId(string value)
    {
        Value = value;
    }

    public string Value { get; }

    public static ParticipantResolutionId New()
        => new(ChatBotIdentity.NewUlid());

    public static bool TryParse(string? value, out ParticipantResolutionId resolutionId)
    {
        if (ChatBotIdentity.TryNormalizeUlid(value, out string? normalized))
        {
            resolutionId = new ParticipantResolutionId(normalized);
            return true;
        }

        resolutionId = default;
        return false;
    }
}
