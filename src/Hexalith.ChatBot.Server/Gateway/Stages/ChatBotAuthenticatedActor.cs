using System.Security.Claims;

namespace Hexalith.ChatBot.Server.Gateway.Stages;

internal sealed record ChatBotAuthenticatedActor(
    string ActorId,
    ClaimsPrincipal Principal,
    string ActorType = "user",
    string? ServiceClientId = null);
