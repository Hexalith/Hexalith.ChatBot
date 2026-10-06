using Dapr.Client;

namespace Hexalith.ChatBot.Server.Gateway.Idempotency;

internal sealed class DaprCoarseIdempotencyStateClient(DaprClient client) : ICoarseIdempotencyStateClient
{
    private const string StateStoreName = "chatbot-statestore";

    /// <summary>Gets the DAPR client whose serializer persists the records.</summary>
    internal DaprClient Client => client;

    private static readonly StateOptions StateOptions = new()
    {
        Consistency = ConsistencyMode.Strong,
        Concurrency = ConcurrencyMode.FirstWrite,
    };

    public async Task<(CoarseIdempotencyRecord? Record, string Etag)> ReadDomainAsync(string key, CancellationToken cancellationToken)
    {
        (CoarseIdempotencyRecord? record, string etag) = await client.GetStateAndETagAsync<CoarseIdempotencyRecord>(
            StateStoreName, key, ConsistencyMode.Strong, metadata: null, cancellationToken).ConfigureAwait(false);
        return (record, etag);
    }

    public async Task<(CoarseCommandIdentityRecord? Record, string Etag)> ReadIdentityAsync(string key, CancellationToken cancellationToken)
    {
        (CoarseCommandIdentityRecord? record, string etag) = await client.GetStateAndETagAsync<CoarseCommandIdentityRecord>(
            StateStoreName, key, ConsistencyMode.Strong, metadata: null, cancellationToken).ConfigureAwait(false);
        return (record, etag);
    }

    public Task<bool> TrySaveDomainAsync(string key, CoarseIdempotencyRecord record, string etag, CancellationToken cancellationToken)
        => client.TrySaveStateAsync(StateStoreName, key, record, etag, StateOptions, metadata: null, cancellationToken);

    public Task<bool> TrySaveIdentityAsync(string key, CoarseCommandIdentityRecord record, string etag, CancellationToken cancellationToken)
        => client.TrySaveStateAsync(StateStoreName, key, record, etag, StateOptions, metadata: null, cancellationToken);

    public Task<bool> TryDeleteAsync(string key, string etag, CancellationToken cancellationToken)
        => client.TryDeleteStateAsync(StateStoreName, key, etag, StateOptions, metadata: null, cancellationToken);
}
