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
    private long _version;

    /// <summary>Gets the number of successful persisted writes.</summary>
    public int Writes { get; private set; }

    /// <summary>Gets the number of persisted reads.</summary>
    public int Reads { get; private set; }

    /// <summary>Gets the number of successful persisted erases.</summary>
    public int Erases { get; private set; }

    /// <inheritdoc />
    public Task<ReadModelEntry<TValue>> GetAsync<TValue>(
        string storeName,
        string key,
        CancellationToken cancellationToken = default)
        where TValue : class
    {
        cancellationToken.ThrowIfCancellationRequested();
        Reads++;
        return Task.FromResult(
            _entries.TryGetValue(CompositeKey(storeName, key), out Entry? entry)
                ? new ReadModelEntry<TValue>((TValue)entry.Value, entry.ETag)
                : new ReadModelEntry<TValue>(null, null));
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
        _entries[CompositeKey(storeName, key)] = new Entry(value, NextEtag());
        Writes++;
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
                    Writes++;
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
                Writes++;
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
            Erases++;
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

    private string NextEtag()
        => Interlocked.Increment(ref _version).ToString(System.Globalization.CultureInfo.InvariantCulture);

    private sealed record Entry(object Value, string ETag);
}
