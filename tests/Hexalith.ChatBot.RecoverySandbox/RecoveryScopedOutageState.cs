namespace Hexalith.ChatBot.RecoverySandbox;

/// <summary>Thread-safe fault and idempotent-effect state for the four exercised ChatBot dependency seams.</summary>
internal sealed class RecoveryScopedOutageState
{
    private static readonly HashSet<string> Dependencies =
    [
        "ai-provider",
        "command-execution",
        "audit-store",
        "attachment-processing",
    ];

    private readonly object _gate = new();
    private readonly HashSet<string> _faulted = new(StringComparer.Ordinal);
    private readonly Dictionary<string, DateTimeOffset> _faultedAtUtc = new(StringComparer.Ordinal);
    private readonly Dictionary<string, DateTimeOffset> _restoredAtUtc = new(StringComparer.Ordinal);
    private readonly Dictionary<string, Dictionary<string, HashSet<string>>> _completedEffects = new(StringComparer.Ordinal);
    private readonly Dictionary<string, int> _faultObservations = new(StringComparer.Ordinal);

    /// <summary>Returns whether this state owns the dependency token.</summary>
    public static bool Contains(string dependency) => Dependencies.Contains(dependency);

    /// <summary>Returns whether the selected dependency is currently faulted.</summary>
    public bool IsFaulted(string dependency)
    {
        lock (_gate)
        {
            return _faulted.Contains(dependency);
        }
    }

    /// <summary>Faults the selected dependency.</summary>
    public object Fault(string dependency, DateTimeOffset atUtc)
    {
        lock (_gate)
        {
            _ = _faulted.Add(dependency);
            _faultedAtUtc[dependency] = atUtc.ToUniversalTime();
            return SnapshotCore(dependency);
        }
    }

    /// <summary>
    /// Restores the dependency and resets completed effects for a fresh idempotency exercise. Returns the state as
    /// observed immediately before clearing the fault — not a post-clear snapshot, which would always report
    /// <c>faulted: false</c> by construction and make a pre-injection/post-cleanup dirty-boundary check unreachable.
    /// </summary>
    public object Restore(string dependency, DateTimeOffset atUtc)
    {
        lock (_gate)
        {
            object priorState = SnapshotCore(dependency);
            _ = _faulted.Remove(dependency);
            _restoredAtUtc[dependency] = atUtc.ToUniversalTime();
            _completedEffects.Remove(dependency);
            return priorState;
        }
    }

    /// <summary>Records one fault observed through the selected dependency contract.</summary>
    public void RecordFaultObservation(string dependency)
    {
        lock (_gate)
        {
            _faultObservations[dependency] = _faultObservations.GetValueOrDefault(dependency) + 1;
        }
    }

    /// <summary>Records an idempotent metadata-only effect and returns its per-correlation emission count.</summary>
    public int RecordEffect(string dependency, string tenantRef, string correlationId)
    {
        lock (_gate)
        {
            if (!_completedEffects.TryGetValue(dependency, out Dictionary<string, HashSet<string>>? tenants))
            {
                tenants = new Dictionary<string, HashSet<string>>(StringComparer.Ordinal);
                _completedEffects[dependency] = tenants;
            }

            if (!tenants.TryGetValue(tenantRef, out HashSet<string>? effects))
            {
                effects = new HashSet<string>(StringComparer.Ordinal);
                tenants[tenantRef] = effects;
            }

            _ = effects.Add(correlationId);
            return effects.Count(effect => string.Equals(effect, correlationId, StringComparison.Ordinal));
        }
    }

    /// <summary>Returns the number of completed effects for one dependency and tenant.</summary>
    public int EffectCount(string dependency, string tenantRef)
    {
        lock (_gate)
        {
            return _completedEffects.TryGetValue(dependency, out Dictionary<string, HashSet<string>>? tenants) &&
                tenants.TryGetValue(tenantRef, out HashSet<string>? effects)
                ? effects.Count
                : 0;
        }
    }

    /// <summary>Returns whether the exercise wrote an effect outside its configured tenant.</summary>
    public bool HasCrossTenantEffect(string dependency, string tenantRef)
    {
        lock (_gate)
        {
            return _completedEffects.TryGetValue(dependency, out Dictionary<string, HashSet<string>>? tenants) &&
                tenants.Any(pair => !string.Equals(pair.Key, tenantRef, StringComparison.Ordinal) && pair.Value.Count > 0);
        }
    }

    /// <summary>Returns current metadata-only state derived from recorded operations.</summary>
    public object Snapshot(string dependency)
    {
        lock (_gate)
        {
            return SnapshotCore(dependency);
        }
    }

    private object SnapshotCore(string dependency)
        => new
        {
            faulted = _faulted.Contains(dependency),
            faultedAtUtc = _faultedAtUtc.GetValueOrDefault(dependency),
            restoredAtUtc = _restoredAtUtc.GetValueOrDefault(dependency),
            faultObservations = _faultObservations.GetValueOrDefault(dependency),
            effectCount = _completedEffects.TryGetValue(dependency, out Dictionary<string, HashSet<string>>? tenants)
                ? tenants.Values.Sum(static effects => effects.Count)
                : 0,
        };
}
