using Hexalith.ChatBot.Contracts.Identities;
using Hexalith.ChatBot.Contracts.Enums;
using Hexalith.ChatBot.Server.Audit;

namespace Hexalith.ChatBot.Server.Gateway.Stages;

internal sealed class ServiceClientGrantProjectionCache(ISystemClock clock)
{
    public static readonly TimeSpan NormalGrantStaleness = TimeSpan.FromMinutes(5);
    public static readonly TimeSpan RevocationStaleness = TimeSpan.FromSeconds(60);

    private readonly Dictionary<string, CacheEntry> _entries = new(StringComparer.Ordinal);

    public void Upsert(ServiceClientGrant grant)
    {
        ArgumentNullException.ThrowIfNull(grant);
        _entries[Key(grant.TenantId, grant.ServiceClientId, ChatBotSurfaceOrigins.ToWireValue(grant.SurfaceOrigin), grant.GrantId)] =
            new CacheEntry(grant, clock.UtcNow, RevocationInvalidatedAt: null);
    }

    public ServiceClientGrant? TryGet(string tenantId, string serviceClientId, string surfaceOrigin, string grantId)
    {
        if (!_entries.TryGetValue(Key(tenantId, serviceClientId, surfaceOrigin, grantId), out CacheEntry? entry))
        {
            return null;
        }

        DateTimeOffset now = clock.UtcNow;
        if (entry.RevocationInvalidatedAt is { } revokedAt && now - revokedAt >= RevocationStaleness)
        {
            _ = _entries.Remove(Key(tenantId, serviceClientId, surfaceOrigin, grantId));
            return null;
        }

        if (now - entry.CachedAt >= NormalGrantStaleness)
        {
            _ = _entries.Remove(Key(tenantId, serviceClientId, surfaceOrigin, grantId));
            return null;
        }

        return entry.Grant;
    }

    public void InvalidateRevocation(string tenantId, string serviceClientId, string surfaceOrigin, string grantId)
    {
        string key = Key(tenantId, serviceClientId, surfaceOrigin, grantId);
        if (_entries.TryGetValue(key, out CacheEntry? entry))
        {
            _entries[key] = entry with { RevocationInvalidatedAt = clock.UtcNow };
        }
    }

    private static string Key(string tenantId, string serviceClientId, string surfaceOrigin, string grantId)
        => string.Join('|', tenantId, serviceClientId, surfaceOrigin, grantId);

    private sealed record CacheEntry(ServiceClientGrant Grant, DateTimeOffset CachedAt, DateTimeOffset? RevocationInvalidatedAt);
}
