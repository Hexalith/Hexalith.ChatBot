using Hexalith.ChatBot.Server.Gateway.Status;

namespace Hexalith.ChatBot.Server.Tests.Gateway;

internal sealed class DelayedInitialOperationStatusStore(bool failInitialWrite) : IOperationStatusStore
{
    private readonly InMemoryOperationStatusStore _inner = new();
    private readonly TaskCompletionSource _firstWriteEntered = new(TaskCreationOptions.RunContinuationsAsynchronously);
    private readonly TaskCompletionSource _releaseFirstWrite = new(TaskCreationOptions.RunContinuationsAsynchronously);
    private int _writes;

    public Task FirstWriteEntered => _firstWriteEntered.Task;

    public void ReleaseFirstWrite() => _releaseFirstWrite.TrySetResult();

    public async ValueTask UpsertAsync(OperationStatusRecord record, CancellationToken cancellationToken)
    {
        if (Interlocked.Increment(ref _writes) == 1)
        {
            _firstWriteEntered.TrySetResult();
            await _releaseFirstWrite.Task.WaitAsync(cancellationToken).ConfigureAwait(false);
            if (failInitialWrite)
            {
                throw new InvalidOperationException("Injected initial status write failure.");
            }
        }
        await _inner.UpsertAsync(record, cancellationToken).ConfigureAwait(false);
    }

    public ValueTask<OperationStatusRecord?> TryGetAsync(string tenantId, string operationId, CancellationToken cancellationToken)
        => _inner.TryGetAsync(tenantId, operationId, cancellationToken);

    public ValueTask UpdateAsync(string tenantId, string operationId,
        Func<OperationStatusRecord?, OperationStatusRecord?> update, CancellationToken cancellationToken)
        => _inner.UpdateAsync(tenantId, operationId, update, cancellationToken);
}
