namespace Hexalith.ChatBot.Mcp;

public sealed record ChatBotMcpInvocation(
    string ToolName,
    IReadOnlyDictionary<string, object?> Arguments)
{
    public static ChatBotMcpInvocation Create(string toolName, IReadOnlyDictionary<string, object?>? arguments)
        => new(toolName, arguments ?? new Dictionary<string, object?>(StringComparer.Ordinal));
}
