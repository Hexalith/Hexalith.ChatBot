namespace Hexalith.ChatBot.Cli;

public sealed record ChatBotCliOptions(
    bool Json,
    string? CorrelationId,
    string? TaskId,
    string? Tenant);
