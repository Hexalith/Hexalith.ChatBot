using Hexalith.ChatBot.Server.Audit;
using Hexalith.ChatBot.Server.Observability;

namespace Hexalith.ChatBot.Server.Gateway;

/// <summary>
/// Thread-safe, in-process default <see cref="IAuthorizationFailureCounter"/> (Story 8.4, AC5). It keeps a per-tenant
/// list of failure timestamps behind a lock and prunes entries outside the rolling window (default
/// <see cref="AuthorizationFailureSpikeEvaluator.DefaultAuthFailureWindowSeconds"/>) on every <see cref="Record"/> and
/// <see cref="ReadAndReset"/>. The window slides rather than resets, so a sustained spike keeps being reported. The
/// injected <see cref="ISystemClock"/> provides the reference instant for read-time pruning, keeping behaviour
/// deterministic under test. Only the tenant id is ever stored — never an actor, command, or reason (NFR2).
/// </summary>
internal sealed class InMemoryAuthorizationFailureCounter : IAuthorizationFailureCounter
{
    private readonly ISystemClock _clock;
    private readonly int _windowSeconds;
    private readonly Lock _gate = new();
    private readonly Dictionary<string, List<DateTimeOffset>> _failuresByTenant = new(StringComparer.Ordinal);

    public InMemoryAuthorizationFailureCounter(
        ISystemClock clock,
        int windowSeconds = AuthorizationFailureSpikeEvaluator.DefaultAuthFailureWindowSeconds)
    {
        _clock = clock ?? throw new ArgumentNullException(nameof(clock));
        _windowSeconds = windowSeconds;
    }

    public void Record(string tenantId, DateTimeOffset timestamp)
    {
        if (string.IsNullOrWhiteSpace(tenantId))
        {
            return;
        }

        DateTimeOffset stamp = timestamp.ToUniversalTime();
        lock (_gate)
        {
            if (!_failuresByTenant.TryGetValue(tenantId, out List<DateTimeOffset>? timestamps))
            {
                timestamps = [];
                _failuresByTenant[tenantId] = timestamps;
            }

            timestamps.Add(stamp);
            Prune(timestamps, stamp);
        }
    }

    public IReadOnlyList<AuthorizationFailureReading> ReadAndReset()
    {
        DateTimeOffset now = _clock.UtcNow.ToUniversalTime();
        List<AuthorizationFailureReading> readings = [];
        lock (_gate)
        {
            foreach ((string tenantId, List<DateTimeOffset> timestamps) in _failuresByTenant)
            {
                Prune(timestamps, now);
                if (timestamps.Count == 0)
                {
                    continue;
                }

                readings.Add(new AuthorizationFailureReading(tenantId, timestamps.Count, timestamps.Min()));
            }
        }

        return readings;
    }

    private void Prune(List<DateTimeOffset> timestamps, DateTimeOffset reference)
    {
        DateTimeOffset cutoff = reference.AddSeconds(-_windowSeconds);
        timestamps.RemoveAll(timestamp => timestamp < cutoff);
    }
}
