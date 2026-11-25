using Hexalith.ChatBot.Server.Gateway.Status;
using Hexalith.ChatBot.Server.Tests.Gateway.Status;

namespace Hexalith.ChatBot.Server.Tests.Gateway;

internal sealed class DelayedInitialOperationStatusStore(bool failInitialWrite, bool sdk = false) : IOperationStatusStore
{
    private readonly IOperationStatusStore _inner = sdk
        ? new ReadModelOperationStatusStore(new AtomicOperationStatusReadModelStore())
        : new InMemoryOperationStatusStore();
    private readonly TaskCompletionSource _firstWriteEntered = new(TaskCreationOptions.RunContinuationsAsynchronously);
    private readonly TaskCompletionSource _releaseFirstWrite = new(TaskCreationOptions.RunContinuationsAsynchronously);
    private int _writes;

    public Task FirstWriteEntered => _firstWriteEntered.Task;

    public void ReleaseFirstWrite() => _releaseFirstWrite.TrySetResult();

    public ValueTask UpsertAsync(OperationStatusRecord record, CancellationToken cancellationToken)
        => _inner.UpsertAsync(record, cancellationToken);

    public async ValueTask UpdateAsync(string tenantId, string operationId,
        Func<OperationStatusRecord?, OperationStatusRecord?> update, CancellationToken cancellationToken)
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
        await _inner.UpdateAsync(tenantId, operationId, update, cancellationToken).ConfigureAwait(false);
    }

    public ValueTask<OperationStatusRecord?> TryGetAsync(string tenantId, string operationId, CancellationToken cancellationToken)
        => _inner.TryGetAsync(tenantId, operationId, cancellationToken);

}
