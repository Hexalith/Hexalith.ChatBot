using System.Collections.Concurrent;

using Hexalith.ChatBot.Contracts.Identities;
using Hexalith.ChatBot.Contracts.Enums;
using Hexalith.ChatBot.Server.Audit;
using Hexalith.ChatBot.Server.Authorization;

namespace Hexalith.ChatBot.Server.Gateway.Stages;

/// <summary>Exact scoped owner evidence cache with immediate known-revocation tombstones.</summary>
internal sealed class ServiceClientGrantProjectionCache(ISystemClock clock)
{
    /// <summary>The maximum ordinary evidence age.</summary>
    public static readonly TimeSpan NormalGrantStaleness = TimeSpan.FromMinutes(5);
    /// <summary>The maximum time since a current revocation check.</summary>
    public static readonly TimeSpan RevocationStaleness = TimeSpan.FromSeconds(60);
    private readonly ConcurrentDictionary<ChatBotOwnerAuthorityRequest, ChatBotOwnerAuthorityEvidence> _entries = new();
    private readonly ConcurrentDictionary<(string, string, string, string), byte> _revocations = new();

    /// <summary>Stores owner-observed evidence; supplied token grants cannot refresh authority.</summary>
    public void Upsert(ChatBotOwnerAuthorityEvidence evidence)
    {
        ArgumentNullException.ThrowIfNull(evidence);
        if (evidence.ServiceGrant is not { } grant || evidence.Request.Authority != "service-grant")
        {
            return;
        }

        if (evidence.IsRevoked || grant.IsRevoked)
        {
            InvalidateRevocation(grant.TenantId, grant.ServiceClientId, ChatBotSurfaceOrigins.ToWireValue(grant.SurfaceOrigin), grant.GrantId);
        }
        else if (!IsRevoked(evidence.Request, grant.GrantId))
        {
            _entries[evidence.Request] = evidence;
        }
    }

    /// <summary>Returns exact evidence only within both observation and revocation bounds.</summary>
    public ChatBotOwnerAuthorityEvidence? TryGetEvidence(ChatBotOwnerAuthorityRequest request)
    {
        if (!_entries.TryGetValue(request, out ChatBotOwnerAuthorityEvidence? evidence) || evidence.ServiceGrant is not { } grant)
        {
            return null;
        }

        DateTimeOffset now = clock.UtcNow;
        if (evidence.IsRevoked || grant.IsRevoked || IsRevoked(request, grant.GrantId) ||
            evidence.ObservedAt > now || evidence.RevocationCheckedAt > now || evidence.ExpiresAt <= now || grant.ExpiresAt <= now ||
            now - evidence.ObservedAt >= NormalGrantStaleness || now - evidence.RevocationCheckedAt >= RevocationStaleness)
        {
            _entries.TryRemove(request, out _);
            return null;
        }

        return evidence;
    }

    /// <summary>Immediately denies known revocation, scoped by tenant, client, origin and grant.</summary>
    public void InvalidateRevocation(string tenantId, string serviceClientId, string surfaceOrigin, string grantId)
        => _revocations[(tenantId, serviceClientId, surfaceOrigin, grantId)] = 0;

    /// <summary>Checks revocation without extending the cache.</summary>
    public bool IsRevoked(ChatBotOwnerAuthorityRequest request, string? grantId = null)
        => _revocations.ContainsKey((request.TenantId, request.ResourceId, ChatBotSurfaceOrigins.ToWireValue(request.Origin), grantId ?? string.Empty)) ||
            _revocations.ContainsKey((request.TenantId, request.ResourceId, ChatBotSurfaceOrigins.ToWireValue(request.Origin), string.Empty));
}
