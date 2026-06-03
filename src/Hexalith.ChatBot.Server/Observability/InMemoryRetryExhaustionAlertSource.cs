using System.Collections.Concurrent;

namespace Hexalith.ChatBot.Server.Observability;

/// <summary>
/// Thread-safe, in-process default <see cref="IRetryExhaustionAlertSource"/> (Story 8.4, AC2). It tracks the set of
/// tenants for which a retry-exhausted terminal state was signalled since the previous per-tenant read. <see cref="Signal"/>
/// is non-throwing and lock-free; <see cref="ReadAndClear"/> atomically removes and returns a single tenant's flag.
/// Blank tenant ids are ignored (never fabricated into an identity), honouring the fail-safe doctrine.
/// </summary>
internal sealed class InMemoryRetryExhaustionAlertSource : IRetryExhaustionAlertSource
{
    private readonly ConcurrentDictionary<string, byte> _signalledTenants = new(StringComparer.Ordinal);

    public void Signal(string tenantId)
    {
        if (string.IsNullOrWhiteSpace(tenantId))
        {
            return;
        }

        _signalledTenants[tenantId] = 0;
    }

    public bool ReadAndClear(string tenantId)
        => !string.IsNullOrWhiteSpace(tenantId) && _signalledTenants.TryRemove(tenantId, out _);
}
