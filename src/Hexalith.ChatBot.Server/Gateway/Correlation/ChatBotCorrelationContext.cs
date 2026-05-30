namespace Hexalith.ChatBot.Server.Gateway.Correlation;

internal sealed record ChatBotCorrelationContext(string CorrelationId, string? TaskId);
