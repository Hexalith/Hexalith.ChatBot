namespace Hexalith.ChatBot.Contracts.Identities;

public readonly record struct ChatBotCommandId
{
    private ChatBotCommandId(string value)
    {
        Value = value;
    }

    public string Value { get; }

    public static ChatBotCommandId New()
        => new(ChatBotIdentity.NewUlid());

    public static bool TryParse(string? value, out ChatBotCommandId commandId)
    {
        if (ChatBotIdentity.TryNormalizeUlid(value, out string? normalized))
        {
            commandId = new ChatBotCommandId(normalized);
            return true;
        }

        commandId = default;
        return false;
    }

    public override string ToString()
        => Value;
}
