using Hexalith.EventStore.Client.Projections;

namespace Hexalith.ChatBot.Server.Tests.Gateway.Status;

/// <summary>Synthetic SDK store with real conditional replacement; makes no native durability claim.</summary>
internal sealed class AtomicOperationStatusReadModelStore : IReadModelStore
{
    private readonly Lock _sync = new();
    private object? _value;
    private long _revision;
    private int _racingSaves;
    private readonly TaskCompletionSource _ready = new(TaskCreationOptions.RunContinuationsAsynchronously);

    public bool RaceNextPair { get; set; }
    public int ConditionalConflicts { get; private set; }

    public Task<ReadModelEntry<TValue>> GetAsync<TValue>(string storeName, string key, CancellationToken cancellationToken = default)
        where TValue : class
    {
        cancellationToken.ThrowIfCancellationRequested();
        lock (_sync)
        {
            return Task.FromResult(new ReadModelEntry<TValue>((TValue?)_value, _value is null ? null : _revision.ToString(System.Globalization.CultureInfo.InvariantCulture)));
        }
    }

    public Task SaveAsync<TValue>(string storeName, string key, TValue value, CancellationToken cancellationToken = default)
        where TValue : class => throw new InvalidOperationException("Direct writes are forbidden.");

    public async Task<bool> TrySaveAsync<TValue>(string storeName, string key, TValue value, string etag, CancellationToken cancellationToken = default)
        where TValue : class
    {
        if (RaceNextPair)
        {
            int attempt = Interlocked.Increment(ref _racingSaves);
            if (attempt == 2)
            {
                _ready.TrySetResult();
            }
            if (attempt <= 2)
            {
                await _ready.Task.WaitAsync(cancellationToken).ConfigureAwait(false);
            }
        }
        lock (_sync)
        {
            string current = _value is null ? string.Empty : _revision.ToString(System.Globalization.CultureInfo.InvariantCulture);
            if (current != etag)
            {
                ConditionalConflicts++;
                return false;
            }
            _value = value;
            _revision++;
            return true;
        }
    }
}
