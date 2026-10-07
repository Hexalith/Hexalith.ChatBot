using System.Security.Claims;

using Hexalith.ChatBot.Contracts.Enums;

namespace Hexalith.ChatBot.Server.Authentication;

/// <summary>An immutable snapshot of authenticated identity, separate from declared provenance.</summary>
internal sealed class ChatBotRequestContext
{
    private readonly ClaimsPrincipal _principal;

    /// <summary>Creates a bound snapshot; only the resolver supplies authenticated identities.</summary>
    internal ChatBotRequestContext(string subjectId, string? tenantId, string actorClass, string? serviceClientId, ChatBotSurfaceOrigin origin, ClaimsPrincipal principal)
    {
        SubjectId = subjectId;
        TenantId = tenantId;
        ActorClass = actorClass;
        ServiceClientId = serviceClientId;
        Origin = origin;
        _principal = new ClaimsPrincipal(principal.Identities.Select(static identity => identity.Clone()));
    }

    /// <summary>The authenticated subject, never a submitted actor identifier.</summary>
    public string SubjectId { get; }
    /// <summary>The unambiguous authenticated tenant, or null until tenant binding denies it.</summary>
    public string? TenantId { get; }
    /// <summary>The bound human, service or AI class.</summary>
    public string ActorClass { get; }
    /// <summary>The explicit machine client identity, independent of OAuth azp.</summary>
    public string? ServiceClientId { get; }
    /// <summary>The immutable adapter declaration; this confers no authority.</summary>
    public ChatBotSurfaceOrigin Origin { get; }
    /// <summary>Whether the actor must use scoped machine grants.</summary>
    public bool IsMachine => ActorClass is "service" or "ai";
    /// <summary>Returns a copy so later principal mutations cannot change the snapshot.</summary>
    public ClaimsPrincipal Principal => new(_principal.Identities.Select(static identity => identity.Clone()));
}
