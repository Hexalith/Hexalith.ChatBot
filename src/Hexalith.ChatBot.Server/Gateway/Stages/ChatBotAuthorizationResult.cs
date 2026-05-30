namespace Hexalith.ChatBot.Server.Gateway.Stages;

internal sealed record ChatBotAuthorizationResult(bool IsAllowed, string ReasonCode)
{
    public static ChatBotAuthorizationResult Allowed()
        => new(true, string.Empty);

    public static ChatBotAuthorizationResult Denied(string reasonCode)
        => new(false, reasonCode);
}
