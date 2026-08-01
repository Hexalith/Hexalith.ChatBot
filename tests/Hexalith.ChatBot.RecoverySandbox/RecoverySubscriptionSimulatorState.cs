namespace Hexalith.ChatBot.RecoverySandbox;

/// <summary>Thread-safe metadata-only state for the controlled Graph/subscription boundary.</summary>
internal sealed class RecoverySubscriptionSimulatorState
{
    private readonly object _gate = new();
    private bool _faulted;
    private DateTimeOffset? _faultedAtUtc;
    private DateTimeOffset? _restoredAtUtc;
    private int _processingAttempts;
    private int _recoverableFailures;
    private int _submitted;

    /// <summary>Marks the closed subscription boundary as expired.</summary>
    public void Fault(DateTimeOffset atUtc)
    {
        lock (_gate)
        {
            _faulted = true;
            _faultedAtUtc = atUtc.ToUniversalTime();
        }
    }

    /// <summary>Restores the closed subscription boundary.</summary>
    public void Restore(DateTimeOffset atUtc)
    {
        lock (_gate)
        {
            _faulted = false;
            _restoredAtUtc = atUtc.ToUniversalTime();
        }
    }

    /// <summary>Gets whether the simulated subscription is currently expired.</summary>
    public bool IsFaulted()
    {
        lock (_gate)
        {
            return _faulted;
        }
    }

    /// <summary>Records one real Worker processing result without retaining message data.</summary>
    public void RecordProcessing(bool submitted)
    {
        lock (_gate)
        {
            _processingAttempts++;
            if (submitted)
            {
                _submitted++;
            }
            else
            {
                _recoverableFailures++;
            }
        }
    }

    /// <summary>Returns a metadata-only immutable snapshot for the controller response.</summary>
    public object Snapshot()
    {
        lock (_gate)
        {
            return new
            {
                faulted = _faulted,
                faultedAtUtc = _faultedAtUtc,
                restoredAtUtc = _restoredAtUtc,
                processingAttempts = _processingAttempts,
                recoverableFailures = _recoverableFailures,
                submitted = _submitted,
            };
        }
    }
}
