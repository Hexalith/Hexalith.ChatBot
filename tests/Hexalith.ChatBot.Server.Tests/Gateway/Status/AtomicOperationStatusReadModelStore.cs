using Dapr.Client;

using Hexalith.EventStore.Client.Projections;

namespace Hexalith.ChatBot.Server.Tests.Gateway.Status;

/// <summary>Production Dapr read-model adapter over conditional serialized byte state; makes no native durability claim.</summary>
internal sealed class AtomicOperationStatusReadModelStore : IReadModelStore
{
    // The unkeyed production Dapr client uses the builder's default serializer configuration.
    private readonly ConditionalDaprStateClient _client;
    private readonly DaprReadModelStore _store;

    public AtomicOperationStatusReadModelStore()
    {
        using DaprClient serializerClient = new DaprClientBuilder().Build();
        _client = new ConditionalDaprStateClient(serializerClient.JsonSerializerOptions);
        _store = new DaprReadModelStore(_client);
    }

    public bool RaceNextPair { get => _client.RaceNextPair; set => _client.RaceNextPair = value; }
    public int ConditionalConflicts => _client.ConditionalConflicts;
    public string StoredJson(string key) => _client.StoredJson(key);

    public Task<ReadModelEntry<TValue>> GetAsync<TValue>(string storeName, string key, CancellationToken cancellationToken = default)
        where TValue : class => _store.GetAsync<TValue>(storeName, key, cancellationToken);

    public Task SaveAsync<TValue>(string storeName, string key, TValue value, CancellationToken cancellationToken = default)
        where TValue : class => throw new InvalidOperationException("Direct writes are forbidden.");

    public Task<bool> TrySaveAsync<TValue>(string storeName, string key, TValue value, string etag, CancellationToken cancellationToken = default)
        where TValue : class => _store.TrySaveAsync(storeName, key, value, etag, cancellationToken);
}
