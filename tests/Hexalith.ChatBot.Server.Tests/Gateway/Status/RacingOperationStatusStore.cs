using Hexalith.ChatBot.Server.Gateway.Status;

namespace Hexalith.ChatBot.Server.Tests.Gateway.Status;

/// <summary>Coordinates two transforms after they read the same snapshot, including real SDK conditional saves.</summary>
internal sealed class RacingOperationStatusStore(bool sdk) : IOperationStatusStore, IDisposable
{
    private readonly Barrier _ready = new(2);
    private readonly AtomicOperationStatusReadModelStore _backing = new();
    private IOperationStatusStore? _inner;
    private int _callbacks;
    private bool _armed;
    private int _calls;
    private readonly ManualResetEventSlim _secondWritten = new();
    private readonly TaskCompletionSource _firstSnapshotRead = new(TaskCreationOptions.RunContinuationsAsynchronously);

    public bool SecondWriterWins { get; init; }
    public Action? BeforeRetry { get; set; }
    public Task FirstSnapshotRead => _firstSnapshotRead.Task;

    private IOperationStatusStore Inner => _inner ??= sdk ? new ReadModelOperationStatusStore(_backing) : new InMemoryOperationStatusStore();
    public int CallbackCount => _callbacks;
    public int ConditionalConflicts => _backing.ConditionalConflicts;

    public void Arm() => _armed = true;
    public void Dispose()
    {
        _ready.Dispose();
        _secondWritten.Dispose();
    }

    public ValueTask UpsertAsync(OperationStatusRecord record, CancellationToken cancellationToken)
        => Inner.UpsertAsync(record, cancellationToken);

    public ValueTask<OperationStatusRecord?> TryGetAsync(string tenantId, string operationId, CancellationToken cancellationToken)
        => Inner.TryGetAsync(tenantId, operationId, cancellationToken);

    public async ValueTask UpdateAsync(string tenantId, string operationId,
        Func<OperationStatusRecord?, OperationStatusRecord?> update, CancellationToken cancellationToken)
    {
        int call = _armed ? Interlocked.Increment(ref _calls) : 0;
        try
        {
            await Inner.UpdateAsync(tenantId, operationId, current =>
            {
                int callback = _armed ? Interlocked.Increment(ref _callbacks) : 0;
                if (callback is > 0 and <= 2)
                {
                    if (callback == 1) { _firstSnapshotRead.TrySetResult(); }
                    if (!_ready.SignalAndWait(TimeSpan.FromSeconds(10), cancellationToken))
                    {
                        throw new TimeoutException("Both operation-status transforms must read before either can save.");
                    }
                    if (SecondWriterWins && call == 1 && !_secondWritten.Wait(TimeSpan.FromSeconds(10), cancellationToken))
                    {
                        throw new TimeoutException("The competing update must save before the first callback resumes.");
                    }
                }
                if (callback > 2) { BeforeRetry?.Invoke(); }
                return update(current);
            }, cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            if (SecondWriterWins && call == 2) { _secondWritten.Set(); }
        }
    }
}
