using System.Security.Claims;

using Hexalith.ChatBot.Server.Authentication;

namespace Hexalith.ChatBot.Server.Gateway.Stages;

internal sealed record ChatBotAuthenticatedActor(
    string ActorId,
    ClaimsPrincipal Principal,
    string ActorType = "user",
    string? ServiceClientId = null,
    ChatBotRequestContext? RequestContext = null);
