using Hexalith.ChatBot.Contracts.Identities;

namespace Hexalith.ChatBot.Server.Gateway.Stages;

internal sealed record ChatBotAuthorizationResult(
    bool IsAllowed,
    string ReasonCode,
    ServiceClientGrantEvidence? ServiceClientGrantEvidence = null)
{
    public static ChatBotAuthorizationResult Allowed(ServiceClientGrantEvidence? serviceClientGrantEvidence = null)
        => new(true, string.Empty, serviceClientGrantEvidence);

    public static ChatBotAuthorizationResult Denied(string reasonCode)
        => new(false, reasonCode);
}
