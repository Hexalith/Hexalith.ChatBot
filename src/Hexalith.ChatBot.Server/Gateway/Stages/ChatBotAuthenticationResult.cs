namespace Hexalith.ChatBot.Server.Gateway.Stages;

internal sealed record ChatBotAuthenticationResult(ChatBotAuthenticatedActor? Actor, string ReasonCode)
{
    public bool IsAuthenticated => Actor is not null;

    public static ChatBotAuthenticationResult Authenticated(string actorId, System.Security.Claims.ClaimsPrincipal principal)
        => new(new ChatBotAuthenticatedActor(actorId, principal), string.Empty);

    public static ChatBotAuthenticationResult Denied(string reasonCode)
        => new(null, reasonCode);
}
