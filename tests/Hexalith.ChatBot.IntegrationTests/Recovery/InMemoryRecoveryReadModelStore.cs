using System.Collections.Concurrent;

using Hexalith.EventStore.Client.Projections;

namespace Hexalith.ChatBot.IntegrationTests.Recovery;

/// <summary>
/// ETag-aware in-memory read-model store used to verify that the live rebuild executes real persisted-store reads,
/// writes, and cleanup instead of comparing two values assembled in the driver.
/// </summary>
internal sealed class InMemoryRecoveryReadModelStore : IReadModelStore, IReadModelConditionalEraser
{
    private readonly ConcurrentDictionary<string, Entry> _entries = new(StringComparer.Ordinal);
    private int _writes;
    private int _reads;
    private int _erases;
    private long _version;

    /// <summary>When set, the Nth successful write attempt throws before persisting.</summary>
    public int? FailOnWriteNumber { get; set; }

    /// <summary>When true, every write attempt throws before persisting.</summary>
    public bool RejectWrites { get; set; }

    /// <summary>Gets the number of successful persisted writes.</summary>
    public int Writes => Volatile.Read(ref _writes);

    /// <summary>Gets the number of persisted reads.</summary>
    public int Reads => Volatile.Read(ref _reads);

    /// <summary>Gets the number of successful persisted erases.</summary>
    public int Erases => Volatile.Read(ref _erases);

    /// <inheritdoc />
    public Task<ReadModelEntry<TValue>> GetAsync<TValue>(
        string storeName,
        string key,
        CancellationToken cancellationToken = default)
        where TValue : class
    {
        cancellationToken.ThrowIfCancellationRequested();
        Interlocked.Increment(ref _reads);
        if (!_entries.TryGetValue(CompositeKey(storeName, key), out Entry? entry))
        {
            return Task.FromResult(new ReadModelEntry<TValue>(null, null));
        }

        if (entry.Value is not TValue typed)
        {
            throw new InvalidCastException(
                $"Stored read-model value for '{storeName}/{key}' is '{entry.Value.GetType().FullName}', not '{typeof(TValue).FullName}'.");
        }

        return Task.FromResult(new ReadModelEntry<TValue>(typed, entry.ETag));
    }

    /// <inheritdoc />
    public Task SaveAsync<TValue>(
        string storeName,
        string key,
        TValue value,
        CancellationToken cancellationToken = default)
        where TValue : class
    {
        cancellationToken.ThrowIfCancellationRequested();
        ArgumentNullException.ThrowIfNull(value);
        ThrowIfWriteRejected();
        int nextWrite = Volatile.Read(ref _writes) + 1;
        if (FailOnWriteNumber is int failAt && nextWrite == failAt)
        {
            throw new InvalidOperationException("Injected read-model write failure.");
        }

        _entries[CompositeKey(storeName, key)] = new Entry(value, NextEtag());
        Interlocked.Increment(ref _writes);
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task<bool> TrySaveAsync<TValue>(
        string storeName,
        string key,
        TValue value,
        string etag,
        CancellationToken cancellationToken = default)
        where TValue : class
    {
        cancellationToken.ThrowIfCancellationRequested();
        ArgumentNullException.ThrowIfNull(value);
        ArgumentNullException.ThrowIfNull(etag);
        ThrowIfWriteRejected();
        int nextWrite = Volatile.Read(ref _writes) + 1;
        if (FailOnWriteNumber is int failAt && nextWrite == failAt)
        {
            throw new InvalidOperationException("Injected read-model write failure.");
        }

        string compositeKey = CompositeKey(storeName, key);
        while (true)
        {
            if (!_entries.TryGetValue(compositeKey, out Entry? current))
            {
                if (etag.Length != 0)
                {
                    return Task.FromResult(false);
                }

                if (_entries.TryAdd(compositeKey, new Entry(value, NextEtag())))
                {
                    Interlocked.Increment(ref _writes);
                    return Task.FromResult(true);
                }

                continue;
            }

            if (!string.Equals(current.ETag, etag, StringComparison.Ordinal))
            {
                return Task.FromResult(false);
            }

            if (_entries.TryUpdate(compositeKey, new Entry(value, NextEtag()), current))
            {
                Interlocked.Increment(ref _writes);
                return Task.FromResult(true);
            }
        }
    }

    /// <inheritdoc />
    public Task<bool> TryEraseAsync(
        string storeName,
        string key,
        string etag,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ArgumentNullException.ThrowIfNull(etag);
        string compositeKey = CompositeKey(storeName, key);
        if (!_entries.TryGetValue(compositeKey, out Entry? current))
        {
            return Task.FromResult(true);
        }

        if (!string.Equals(current.ETag, etag, StringComparison.Ordinal))
        {
            return Task.FromResult(false);
        }

        bool removed = _entries.TryRemove(new KeyValuePair<string, Entry>(compositeKey, current));
        if (removed)
        {
            Interlocked.Increment(ref _erases);
        }

        return Task.FromResult(removed);
    }

    /// <inheritdoc />
    public Task<(bool Present, string Etag)> TryReadEtagAsync(
        string storeName,
        string key,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult(
            _entries.TryGetValue(CompositeKey(storeName, key), out Entry? entry)
                ? (true, entry.ETag)
                : (false, string.Empty));
    }

    private static string CompositeKey(string storeName, string key)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(storeName);
        ArgumentException.ThrowIfNullOrWhiteSpace(key);
        return $"{storeName}\u001f{key}";
    }

    private void ThrowIfWriteRejected()
    {
        if (RejectWrites)
        {
            throw new InvalidOperationException("Injected read-model write failure.");
        }
    }

    private string NextEtag()
        => Interlocked.Increment(ref _version).ToString(System.Globalization.CultureInfo.InvariantCulture);

    private sealed record Entry(object Value, string ETag);
}
