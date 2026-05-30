namespace Hexalith.ChatBot.Server.Gateway.Stages;

internal sealed record ChatBotApprovalResult
{
    public static ChatBotApprovalResult Approved { get; } = new();
}
