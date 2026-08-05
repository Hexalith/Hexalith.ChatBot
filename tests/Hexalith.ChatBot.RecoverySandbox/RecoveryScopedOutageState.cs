namespace Hexalith.ChatBot.RecoverySandbox;

/// <summary>Thread-safe fault and effect-emission state for the four exercised ChatBot dependency seams.</summary>
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
    private readonly Dictionary<string, Dictionary<string, Dictionary<string, int>>> _completedEffects = new(StringComparer.Ordinal);
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
    /// Restores the dependency and resets completed effects for a fresh idempotency exercise. Returns the pre-clear
    /// <c>prior</c> snapshot (so dirty-boundary checks stay reachable), the post-clear <c>current</c> snapshot (so
    /// callers can assert cleanliness without a follow-up status read when they only hold this body), and
    /// <c>crossTenantEffectDetectedBeforeRestore</c> — whether any tenant other than <paramref name="expectedTenantRef"/>
    /// had a recorded effect before the ledger was cleared. A leak that happened during the fault window is otherwise
    /// erased by this same clear before a caller can observe it.
    /// </summary>
    public object Restore(string dependency, string expectedTenantRef, DateTimeOffset atUtc)
    {
        lock (_gate)
        {
            object prior = SnapshotCore(dependency);
            bool crossTenantEffectDetectedBeforeRestore = HasCrossTenantEffect(dependency, expectedTenantRef);
            _ = _faulted.Remove(dependency);
            _restoredAtUtc[dependency] = atUtc.ToUniversalTime();
            _completedEffects.Remove(dependency);
            return new { prior, current = SnapshotCore(dependency), crossTenantEffectDetectedBeforeRestore };
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

    /// <summary>Records one effect emission and returns the per-correlation emission count after recording.</summary>
    public int RecordEffect(string dependency, string tenantRef, string correlationId)
    {
        lock (_gate)
        {
            if (!_completedEffects.TryGetValue(dependency, out Dictionary<string, Dictionary<string, int>>? tenants))
            {
                tenants = new Dictionary<string, Dictionary<string, int>>(StringComparer.Ordinal);
                _completedEffects[dependency] = tenants;
            }

            if (!tenants.TryGetValue(tenantRef, out Dictionary<string, int>? effects))
            {
                effects = new Dictionary<string, int>(StringComparer.Ordinal);
                tenants[tenantRef] = effects;
            }

            int count = effects.GetValueOrDefault(correlationId) + 1;
            effects[correlationId] = count;
            return count;
        }
    }

    /// <summary>Returns the total number of completed effect emissions for one dependency and tenant.</summary>
    public int EffectCount(string dependency, string tenantRef)
    {
        lock (_gate)
        {
            return _completedEffects.TryGetValue(dependency, out Dictionary<string, Dictionary<string, int>>? tenants) &&
                tenants.TryGetValue(tenantRef, out Dictionary<string, int>? effects)
                ? effects.Values.Sum()
                : 0;
        }
    }

    /// <summary>Returns the emission count for one dependency, tenant, and correlation id.</summary>
    public int CorrelationEffectCount(string dependency, string tenantRef, string correlationId)
    {
        lock (_gate)
        {
            return _completedEffects.TryGetValue(dependency, out Dictionary<string, Dictionary<string, int>>? tenants) &&
                tenants.TryGetValue(tenantRef, out Dictionary<string, int>? effects)
                ? effects.GetValueOrDefault(correlationId)
                : 0;
        }
    }

    /// <summary>Returns whether the exercise wrote an effect outside its configured tenant.</summary>
    public bool HasCrossTenantEffect(string dependency, string tenantRef)
    {
        lock (_gate)
        {
            return _completedEffects.TryGetValue(dependency, out Dictionary<string, Dictionary<string, int>>? tenants) &&
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
            effectCount = _completedEffects.TryGetValue(dependency, out Dictionary<string, Dictionary<string, int>>? tenants)
                ? tenants.Values.Sum(static effects => effects.Values.Sum())
                : 0,
        };
}
