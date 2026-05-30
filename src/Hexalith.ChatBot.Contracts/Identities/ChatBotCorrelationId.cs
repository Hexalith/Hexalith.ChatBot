namespace Hexalith.ChatBot.Contracts.Identities;

public readonly record struct ChatBotCorrelationId
{
    private ChatBotCorrelationId(string value)
    {
        Value = value;
    }

    public string Value { get; }

    public static ChatBotCorrelationId New()
        => new(ChatBotIdentity.NewUlid());

    public static bool TryParse(string? value, out ChatBotCorrelationId correlationId)
    {
        if (ChatBotIdentity.TryNormalizeUlid(value, out string? normalized))
        {
            correlationId = new ChatBotCorrelationId(normalized);
            return true;
        }

        correlationId = default;
        return false;
    }

    public override string ToString()
        => Value;
}
