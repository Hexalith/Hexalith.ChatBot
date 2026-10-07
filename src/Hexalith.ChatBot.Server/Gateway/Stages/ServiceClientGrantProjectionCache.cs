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
    private readonly ConcurrentDictionary<(string, string, string, string), DateTimeOffset> _revocations = new();
    private readonly ConcurrentDictionary<(string, string, string), DateTimeOffset> _clientRevocationWatermarks = new();

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
        else if (!evidence.Request.RequireCurrent && !IsRevoked(evidence.Request, grant.GrantId) && !PredatesClientRevocation(evidence))
        {
            _entries.AddOrUpdate(evidence.Request, evidence, (_, previous) =>
                evidence.ObservedAt > previous.ObservedAt ||
                (evidence.ObservedAt == previous.ObservedAt && evidence.RevocationCheckedAt > previous.RevocationCheckedAt)
                    ? evidence : previous);
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
        if (evidence.IsRevoked || grant.IsRevoked || IsRevoked(request, grant.GrantId) || PredatesClientRevocation(evidence) ||
            evidence.ObservedAt > now || evidence.RevocationCheckedAt > now || evidence.ExpiresAt <= now || grant.ExpiresAt <= now ||
            now - evidence.ObservedAt >= NormalGrantStaleness || now - evidence.RevocationCheckedAt >= RevocationStaleness)
        {
            ((ICollection<KeyValuePair<ChatBotOwnerAuthorityRequest, ChatBotOwnerAuthorityEvidence>>)_entries).Remove(new(request, evidence));
            return null;
        }

        return evidence;
    }

    /// <summary>Immediately denies known revocation, scoped by tenant, client, origin and grant.</summary>
    public void InvalidateRevocation(string tenantId, string serviceClientId, string surfaceOrigin, string grantId)
    {
        DateTimeOffset revokedAt = clock.UtcNow;
        _revocations.AddOrUpdate((tenantId, serviceClientId, surfaceOrigin, grantId), revokedAt, (_, previous) => previous > revokedAt ? previous : revokedAt);
        if (grantId.Length == 0)
        {
            _clientRevocationWatermarks.AddOrUpdate((tenantId, serviceClientId, surfaceOrigin), revokedAt, (_, previous) => previous > revokedAt ? previous : revokedAt);
        }
        foreach (var entry in _entries)
        {
            if (entry.Key.TenantId == tenantId && entry.Key.ResourceId == serviceClientId && ChatBotSurfaceOrigins.ToWireValue(entry.Key.Origin) == surfaceOrigin &&
                (grantId.Length == 0 || entry.Value.ServiceGrant?.GrantId == grantId))
            {
                ((ICollection<KeyValuePair<ChatBotOwnerAuthorityRequest, ChatBotOwnerAuthorityEvidence>>)_entries).Remove(entry);
            }
        }
    }

    /// <summary>Clears only client-wide revocation after a newer validated owner grant; exact grants stay revoked.</summary>
    public void ObserveAllowedEvidence(ChatBotOwnerAuthorityEvidence evidence)
    {
        if (!evidence.IsAllowed || evidence.IsRevoked || evidence.ServiceGrant is not { IsRevoked: false } || evidence.Request.Authority != "service-grant")
        {
            return;
        }

        var key = (evidence.Request.TenantId, evidence.Request.ResourceId, ChatBotSurfaceOrigins.ToWireValue(evidence.Request.Origin), string.Empty);
        if (_revocations.TryGetValue(key, out DateTimeOffset revokedAt) && evidence.RevocationCheckedAt > revokedAt)
        {
            ((ICollection<KeyValuePair<(string, string, string, string), DateTimeOffset>>)_revocations).Remove(new(key, revokedAt));
        }
    }

    /// <summary>Rejects evidence observed at or before a client-wide revocation, even after a newer re-grant.</summary>
    public bool PredatesClientRevocation(ChatBotOwnerAuthorityEvidence evidence)
        => _clientRevocationWatermarks.TryGetValue((evidence.Request.TenantId, evidence.Request.ResourceId, ChatBotSurfaceOrigins.ToWireValue(evidence.Request.Origin)), out DateTimeOffset revokedAt) &&
            evidence.RevocationCheckedAt <= revokedAt;

    /// <summary>Checks revocation without extending the cache.</summary>
    public bool IsRevoked(ChatBotOwnerAuthorityRequest request, string? grantId = null)
        => _revocations.ContainsKey((request.TenantId, request.ResourceId, ChatBotSurfaceOrigins.ToWireValue(request.Origin), grantId ?? string.Empty)) ||
            _revocations.ContainsKey((request.TenantId, request.ResourceId, ChatBotSurfaceOrigins.ToWireValue(request.Origin), string.Empty));
}
