using System.Security.Claims;

using Hexalith.ChatBot.Contracts.Enums;
using Hexalith.ChatBot.Server.Authentication;
using Hexalith.ChatBot.Server.Audit;
using Hexalith.ChatBot.Server.Authorization;
using Hexalith.ChatBot.Server.Gateway.Stages;

using Microsoft.AspNetCore.Http;

namespace Hexalith.ChatBot.Tests.TrustedAuthority;

/// <summary>Constructs explicit synthetic trusted contexts without caller-granted authority.</summary>
internal static class TrustedAuthorityFixture
{
    /// <summary>Creates the shared authorizer with the test-owned synthetic owner source.</summary>
    public static ChatBotRequestAuthorizer Authorizer(ISystemClock clock, IChatBotOwnerAuthorityProvider provider)
        => new(new ChatBotAuthorityCatalog(), provider, clock, new ServiceClientGrantProjectionCache(clock));

    /// <summary>A real authenticated subject for the declared actor class.</summary>
    public static ClaimsPrincipal Principal(string tenant = "tenant-alpha", string actorClass = "human", string subject = "actor-alpha")
    {
        List<Claim> claims = [new("sub", subject), new("eventstore:tenant", tenant), new(ParticipantAuthorizationStage.ActorTypeClaim, actorClass)];
        if (actorClass is "service" or "ai")
        {
            claims.Add(new(ClaimsServiceClientGrantResolver.ServiceClientIdClaim, "client-alpha"));
        }

        return new(new ClaimsIdentity(claims, "synthetic-authenticated"));
    }

    /// <summary>Binds through the production resolver.</summary>
    public static ChatBotRequestContext Context(string tenant = "tenant-alpha", string actorClass = "human", ChatBotSurfaceOrigin origin = ChatBotSurfaceOrigin.Api)
    {
        if (!ChatBotRequestContextResolver.TryResolve(Principal(tenant, actorClass), origin, out ChatBotRequestContext? context, out _))
        {
            throw new InvalidOperationException("Invalid synthetic identity.");
        }

        return context!;
    }

    /// <summary>Supplies authenticated transport evidence to the production SDK handler.</summary>
    public static ChatBotRequestContextResolver Resolver(ClaimsPrincipal principal)
        => new(new HttpContextAccessor { HttpContext = new DefaultHttpContext { User = principal } });
}
