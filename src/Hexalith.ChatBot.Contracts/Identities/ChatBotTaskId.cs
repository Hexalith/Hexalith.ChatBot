namespace Hexalith.ChatBot.Contracts.Identities;

public readonly record struct ChatBotTaskId
{
    private ChatBotTaskId(string value)
    {
        Value = value;
    }

    public string Value { get; }

    public static ChatBotTaskId New()
        => new(ChatBotIdentity.NewUlid());

    public static bool TryParse(string? value, out ChatBotTaskId taskId)
    {
        if (ChatBotIdentity.TryNormalizeUlid(value, out string? normalized))
        {
            taskId = new ChatBotTaskId(normalized);
            return true;
        }

        taskId = default;
        return false;
    }

    public override string ToString()
        => Value;
}
