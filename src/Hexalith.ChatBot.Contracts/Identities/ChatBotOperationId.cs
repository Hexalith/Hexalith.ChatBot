namespace Hexalith.ChatBot.Contracts.Identities;

/// <summary>A lifetime operation ULID issued by the caller as its command identity.</summary>
public readonly record struct ChatBotOperationId
{
    private ChatBotOperationId(string value) => Value = value;

    /// <summary>Gets the normalized ULID.</summary>
    public string Value { get; }

    /// <summary>Parses a caller command identity without deriving it from a task.</summary>
    public static bool TryParse(string? value, out ChatBotOperationId operationId)
    {
        if (ChatBotCommandId.TryParse(value, out ChatBotCommandId commandId))
        {
            operationId = new(commandId.Value);
            return true;
        }
        operationId = default;
        return false;
    }

    /// <inheritdoc/>
    public override string ToString() => Value;
}
