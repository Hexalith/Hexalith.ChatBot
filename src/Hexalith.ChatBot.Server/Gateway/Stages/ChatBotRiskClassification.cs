namespace Hexalith.ChatBot.Server.Gateway.Stages;

internal sealed record ChatBotRiskClassification
{
    public static ChatBotRiskClassification PassThrough { get; } = new();
}
