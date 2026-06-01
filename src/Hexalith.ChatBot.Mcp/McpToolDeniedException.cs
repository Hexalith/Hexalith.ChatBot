namespace Hexalith.ChatBot.Mcp;

public sealed class McpToolDeniedException : Exception
{
    private McpToolDeniedException(
        string category,
        string code,
        string safeMessage,
        string clientAction,
        string safeSuggestion,
        string? correlationId,
        string? taskId,
        bool retryable = false)
        : base(safeMessage)
    {
        Category = category;
        Code = code;
        SafeMessage = safeMessage;
        ClientAction = clientAction;
        SafeSuggestion = safeSuggestion;
        CorrelationId = correlationId;
        TaskId = taskId;
        Retryable = retryable;
    }

    public string Category { get; }

    public string Code { get; }

    public string SafeMessage { get; }

    public string ClientAction { get; }

    public string SafeSuggestion { get; }

    public string? CorrelationId { get; }

    public string? TaskId { get; }

    public bool Retryable { get; }

    public static McpToolDeniedException UnknownTool(string toolName)
        => new(
            "validation_error",
            "mcp.tool.unknown",
            "The requested MCP tool is not available.",
            "correct-request",
            $"Use {ChatBotMcpToolCatalog.NearestToolName(toolName)}.",
            correlationId: null,
            taskId: null);

    public static McpToolDeniedException InvalidArgument(
        string code,
        string safeMessage,
        string safeSuggestion,
        string? correlationId = null,
        string? taskId = null)
        => new(
            "validation_error",
            code,
            safeMessage,
            "correct-request",
            safeSuggestion,
            correlationId,
            taskId);
}
