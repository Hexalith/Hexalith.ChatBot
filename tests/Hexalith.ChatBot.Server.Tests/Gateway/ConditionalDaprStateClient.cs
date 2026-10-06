using System.Text.Json;

using Dapr.Client;

using Hexalith.ChatBot.Server.Gateway.Idempotency;

#pragma warning disable DAPR_DISTRIBUTEDLOCK

namespace Hexalith.ChatBot.Server.Tests.Gateway;

/// <summary>
/// Dapr-compatible conditional state seam. First-write concurrency is enforced only when the
/// production client passes <see cref="ConcurrencyMode.FirstWrite"/> on the save call.
/// State crosses the configured JSON byte boundary on every write and read.
/// </summary>
internal sealed class ConditionalDaprStateClient : DaprClient
{
    private readonly Lock _sync = new();
    private readonly Dictionary<string, (byte[] Bytes, int Version)> _records = new(StringComparer.Ordinal);
    private readonly List<IdentitySave> _identitySaves = [];

    public string? BlockNextWriteKind { get; set; }
    public ManualResetEventSlim? OwnershipWriteEntered { get; set; }
    public ManualResetEventSlim? OwnershipWriteRelease { get; set; }
    public int PhysicalDeletes { get; private set; }

    private void GateOwnershipWrite(string kind)
    {
        if (BlockNextWriteKind != kind || OwnershipWriteEntered is null || OwnershipWriteRelease is null)
        {
            return;
        }
        BlockNextWriteKind = null;
        OwnershipWriteEntered.Set();
        if (!OwnershipWriteRelease.Wait(TimeSpan.FromSeconds(15)))
        {
            throw new TimeoutException("The production state write gate was not released.");
        }
    }

    public Barrier? IdentityCreateBarrier { get; init; }

    public IReadOnlyList<IdentitySave> IdentitySaves
    {
        get
        {
            lock (_sync)
            {
                return [.. _identitySaves];
            }
        }
    }

    /// <summary>
    /// Gets the serializer options; defaults to the production options of the dedicated idempotency DAPR client so the
    /// persisted bytes match what production writes.
    /// </summary>
    public override JsonSerializerOptions JsonSerializerOptions { get; } = CoarseIdempotencyStateJson.Options;

    /// <summary>Returns the persisted JSON of a key, exactly as it crossed the byte boundary.</summary>
    public string StoredJson(string key)
    {
        lock (_sync)
        {
            return System.Text.Encoding.UTF8.GetString(_records[key].Bytes);
        }
    }

    public override Task<(TValue value, string etag)> GetStateAndETagAsync<TValue>(
        string storeName,
        string key,
        ConsistencyMode? consistencyMode = null,
        IReadOnlyDictionary<string, string>? metadata = null,
        CancellationToken cancellationToken = default)
    {
        lock (_sync)
        {
            if (_records.TryGetValue(key, out (byte[] Bytes, int Version) current))
            {
                TValue typed = JsonSerializer.Deserialize<TValue>(current.Bytes, JsonSerializerOptions)!;
                return Task.FromResult((typed, current.Version.ToString(System.Globalization.CultureInfo.InvariantCulture)));
            }

            return Task.FromResult((default(TValue)!, string.Empty));
        }
    }

    public override Task<bool> TrySaveStateAsync<TValue>(
        string storeName,
        string key,
        TValue value,
        string etag,
        StateOptions? stateOptions = null,
        IReadOnlyDictionary<string, string>? metadata = null,
        CancellationToken cancellationToken = default)
    {
        byte[] bytes = JsonSerializer.SerializeToUtf8Bytes(value, JsonSerializerOptions);
        if (value is CoarseCommandIdentityRecord { Released: true })
        {
            GateOwnershipWrite("identity-release");
        }
        else if (value is CoarseCommandIdentityRecord { DomainReservation.DispatchState: CoarseDispatchState.Dispatching })
        {
            GateOwnershipWrite("prepare");
        }
        else if (value is CoarseIdempotencyRecord { Released: true })
        {
            GateOwnershipWrite("domain-release");
        }

        if (value is CoarseCommandIdentityRecord { PriorOutcome: null } && string.IsNullOrEmpty(etag))
        {
            IdentityCreateBarrier?.SignalAndWait(TimeSpan.FromSeconds(10), cancellationToken);
        }

        lock (_sync)
        {
            bool firstWrite = stateOptions?.Concurrency == ConcurrencyMode.FirstWrite;
            bool saved;
            if (_records.TryGetValue(key, out (byte[] Bytes, int Version) current))
            {
                string currentEtag = current.Version.ToString(System.Globalization.CultureInfo.InvariantCulture);
                if (firstWrite && etag != currentEtag)
                {
                    saved = false;
                }
                else
                {
                    _records[key] = (bytes, current.Version + 1);
                    saved = true;
                }
            }
            else if (firstWrite && !string.IsNullOrEmpty(etag))
            {
                saved = false;
            }
            else
            {
                _records[key] = (bytes, 1);
                saved = true;
            }

            if (value is CoarseCommandIdentityRecord)
            {
                _identitySaves.Add(new IdentitySave(storeName, stateOptions, etag, saved));
            }

            return Task.FromResult(saved);
        }
    }

    public override Task<bool> TryDeleteStateAsync(
        string storeName,
        string key,
        string etag,
        StateOptions? stateOptions = null,
        IReadOnlyDictionary<string, string>? metadata = null,
        CancellationToken cancellationToken = default)
    {
        lock (_sync)
        {
            PhysicalDeletes++;
            if (_records.TryGetValue(key, out (byte[] Bytes, int Version) current) &&
                etag == current.Version.ToString(System.Globalization.CultureInfo.InvariantCulture))
            {
                _records.Remove(key);
                return Task.FromResult(true);
            }

            return Task.FromResult(false);
        }
    }

    public override Task<TValue> GetStateAsync<TValue>(string storeName, string key, ConsistencyMode? consistencyMode = null, IReadOnlyDictionary<string, string>? metadata = null, CancellationToken cancellationToken = default)
        => throw Unsupported();

    public override Task<IReadOnlyList<BulkStateItem>> GetBulkStateAsync(string storeName, IReadOnlyList<string> keys, int? parallelism, IReadOnlyDictionary<string, string>? metadata = null, CancellationToken cancellationToken = default)
        => throw Unsupported();

    public override Task<IReadOnlyList<BulkStateItem<TValue>>> GetBulkStateAsync<TValue>(string storeName, IReadOnlyList<string> keys, int? parallelism, IReadOnlyDictionary<string, string>? metadata = null, CancellationToken cancellationToken = default)
        => throw Unsupported();

    public override Task SaveBulkStateAsync<TValue>(string storeName, IReadOnlyList<SaveStateItem<TValue>> items, CancellationToken cancellationToken = default)
        => throw Unsupported();

    public override Task DeleteBulkStateAsync(string storeName, IReadOnlyList<BulkDeleteStateItem> items, CancellationToken cancellationToken = default)
        => throw Unsupported();

    public override Task SaveStateAsync<TValue>(string storeName, string key, TValue value, StateOptions? stateOptions = null, IReadOnlyDictionary<string, string>? metadata = null, CancellationToken cancellationToken = default)
        => throw Unsupported();

    public override Task SaveByteStateAsync(string storeName, string key, ReadOnlyMemory<byte> binaryValue, StateOptions? stateOptions = null, IReadOnlyDictionary<string, string>? metadata = null, CancellationToken cancellationToken = default)
        => throw Unsupported();

    public override Task<bool> TrySaveByteStateAsync(string storeName, string key, ReadOnlyMemory<byte> binaryValue, string etag, StateOptions? stateOptions = null, IReadOnlyDictionary<string, string>? metadata = null, CancellationToken cancellationToken = default)
        => throw Unsupported();

    public override Task<ReadOnlyMemory<byte>> GetByteStateAsync(string storeName, string key, ConsistencyMode? consistencyMode = null, IReadOnlyDictionary<string, string>? metadata = null, CancellationToken cancellationToken = default)
        => throw Unsupported();

    public override Task<(ReadOnlyMemory<byte>, string etag)> GetByteStateAndETagAsync(string storeName, string key, ConsistencyMode? consistencyMode = null, IReadOnlyDictionary<string, string>? metadata = null, CancellationToken cancellationToken = default)
        => throw Unsupported();

    public override Task ExecuteStateTransactionAsync(string storeName, IReadOnlyList<StateTransactionRequest> operations, IReadOnlyDictionary<string, string>? metadata = null, CancellationToken cancellationToken = default)
        => throw Unsupported();

    public override Task DeleteStateAsync(string storeName, string key, StateOptions? stateOptions = null, IReadOnlyDictionary<string, string>? metadata = null, CancellationToken cancellationToken = default)
        => throw Unsupported();

    public override Task<StateQueryResponse<TValue>> QueryStateAsync<TValue>(string storeName, string jsonQuery, IReadOnlyDictionary<string, string>? metadata = null, CancellationToken cancellationToken = default)
        => throw Unsupported();

    public override Task<Dictionary<string, string>> GetSecretAsync(string storeName, string key, IReadOnlyDictionary<string, string>? metadata = null, CancellationToken cancellationToken = default)
        => throw Unsupported();

    public override Task<Dictionary<string, Dictionary<string, string>>> GetBulkSecretAsync(string storeName, IReadOnlyDictionary<string, string>? metadata = null, CancellationToken cancellationToken = default)
        => throw Unsupported();

    public override Task<GetConfigurationResponse> GetConfiguration(string storeName, IReadOnlyList<string> keys, IReadOnlyDictionary<string, string>? metadata = null, CancellationToken cancellationToken = default)
        => throw Unsupported();

    public override Task<SubscribeConfigurationResponse> SubscribeConfiguration(string storeName, IReadOnlyList<string> keys, IReadOnlyDictionary<string, string>? metadata = null, CancellationToken cancellationToken = default)
        => throw Unsupported();

    public override Task<UnsubscribeConfigurationResponse> UnsubscribeConfiguration(string storeName, string id, CancellationToken cancellationToken = default)
        => throw Unsupported();

    public override Task<ReadOnlyMemory<byte>> EncryptAsync(string vaultResourceName, ReadOnlyMemory<byte> plaintextBytes, string keyName, EncryptionOptions encryptionOptions, CancellationToken cancellationToken = default)
        => throw Unsupported();

    public override IAsyncEnumerable<ReadOnlyMemory<byte>> EncryptAsync(string vaultResourceName, Stream plaintextStream, string keyName, EncryptionOptions encryptionOptions, CancellationToken cancellationToken = default)
        => throw Unsupported();

    public override Task<ReadOnlyMemory<byte>> DecryptAsync(string vaultResourceName, ReadOnlyMemory<byte> ciphertextBytes, string keyName, DecryptionOptions options, CancellationToken cancellationToken = default)
        => throw Unsupported();

    public override Task<ReadOnlyMemory<byte>> DecryptAsync(string vaultResourceName, ReadOnlyMemory<byte> ciphertextBytes, string keyName, CancellationToken cancellationToken = default)
        => throw Unsupported();

    public override IAsyncEnumerable<ReadOnlyMemory<byte>> DecryptAsync(string vaultResourceName, Stream ciphertextStream, string keyName, DecryptionOptions options, CancellationToken cancellationToken = default)
        => throw Unsupported();

    public override IAsyncEnumerable<ReadOnlyMemory<byte>> DecryptAsync(string vaultResourceName, Stream ciphertextStream, string keyName, CancellationToken cancellationToken = default)
        => throw Unsupported();

    public override Task<TryLockResponse> Lock(string storeName, string resourceId, string lockOwner, int expiryInSeconds, CancellationToken cancellationToken = default)
        => throw Unsupported();

    public override Task<UnlockResponse> Unlock(string storeName, string resourceId, string lockOwner, CancellationToken cancellationToken = default)
        => throw Unsupported();

    public override Task PublishEventAsync<TData>(string pubsubName, string topicName, TData data, CancellationToken cancellationToken = default)
        => throw Unsupported();

    public override Task PublishEventAsync<TData>(string pubsubName, string topicName, TData data, Dictionary<string, string> metadata, CancellationToken cancellationToken = default)
        => throw Unsupported();

    public override Task PublishEventAsync(string pubsubName, string topicName, CancellationToken cancellationToken = default)
        => throw Unsupported();

    public override Task PublishEventAsync(string pubsubName, string topicName, Dictionary<string, string> metadata, CancellationToken cancellationToken = default)
        => throw Unsupported();

    public override Task<BulkPublishResponse<TValue>> BulkPublishEventAsync<TValue>(string pubsubName, string topicName, IReadOnlyList<TValue> events, Dictionary<string, string>? metadata = null, CancellationToken cancellationToken = default)
        => throw Unsupported();

    public override Task PublishByteEventAsync(string pubsubName, string topicName, ReadOnlyMemory<byte> data, string dataContentType = "application/json", Dictionary<string, string>? metadata = null, CancellationToken cancellationToken = default)
        => throw Unsupported();

    public override Task InvokeBindingAsync<TRequest>(string bindingName, string operation, TRequest data, IReadOnlyDictionary<string, string>? metadata = null, CancellationToken cancellationToken = default)
        => throw Unsupported();

    public override Task<TResponse> InvokeBindingAsync<TRequest, TResponse>(string bindingName, string operation, TRequest data, IReadOnlyDictionary<string, string>? metadata = null, CancellationToken cancellationToken = default)
        => throw Unsupported();

    public override Task<BindingResponse> InvokeBindingAsync(BindingRequest request, CancellationToken cancellationToken = default)
        => throw Unsupported();

    public override HttpRequestMessage CreateInvokeMethodRequest(HttpMethod httpMethod, string appId, string methodName)
        => throw Unsupported();

    public override HttpRequestMessage CreateInvokeMethodRequest(HttpMethod httpMethod, string appId, string methodName, IReadOnlyCollection<KeyValuePair<string, string>> queryStringParameters)
        => throw Unsupported();

    public override HttpRequestMessage CreateInvokeMethodRequest<TRequest>(HttpMethod httpMethod, string appId, string methodName, IReadOnlyCollection<KeyValuePair<string, string>> queryStringParameters, TRequest data)
        => throw Unsupported();

    public override Task<bool> CheckHealthAsync(CancellationToken cancellationToken = default)
        => throw Unsupported();

    public override Task<bool> CheckOutboundHealthAsync(CancellationToken cancellationToken = default)
        => throw Unsupported();

    public override Task WaitForSidecarAsync(CancellationToken cancellationToken = default)
        => throw Unsupported();

    public override Task ShutdownSidecarAsync(CancellationToken cancellationToken = default)
        => throw Unsupported();

    public override Task<DaprMetadata> GetMetadataAsync(CancellationToken cancellationToken = default)
        => throw Unsupported();

    public override Task SetMetadataAsync(string attributeName, string attributeValue, CancellationToken cancellationToken = default)
        => throw Unsupported();

    [Obsolete("Recommended guidance is to use a native HTTP or gRPC client for service invocation")]
    public override Task<HttpResponseMessage> InvokeMethodWithResponseAsync(HttpRequestMessage request, CancellationToken cancellationToken = default)
        => throw Unsupported();

    public override HttpClient CreateInvokableHttpClient(string? appId = null)
        => throw Unsupported();

    [Obsolete("Recommended guidance is to use a native HTTP or gRPC client for service invocation")]
    public override Task InvokeMethodAsync(HttpRequestMessage request, CancellationToken cancellationToken = default)
        => throw Unsupported();

    [Obsolete("Recommended guidance is to use a native HTTP or gRPC client for service invocation")]
    public override Task<TResponse> InvokeMethodAsync<TResponse>(HttpRequestMessage request, CancellationToken cancellationToken = default)
        => throw Unsupported();

    [Obsolete("Recommended guidance is to use a native HTTP or gRPC client for service invocation")]
    public override Task InvokeMethodGrpcAsync(string appId, string methodName, CancellationToken cancellationToken = default)
        => throw Unsupported();

    [Obsolete("Recommended guidance is to use a native HTTP or gRPC client for service invocation")]
    public override Task InvokeMethodGrpcAsync<TRequest>(string appId, string methodName, TRequest data, CancellationToken cancellationToken = default)
        => throw Unsupported();

    [Obsolete("Recommended guidance is to use a native HTTP or gRPC client for service invocation")]
    public override Task<TResponse> InvokeMethodGrpcAsync<TResponse>(string appId, string methodName, CancellationToken cancellationToken = default)
        => throw Unsupported();

    [Obsolete("Recommended guidance is to use a native HTTP or gRPC client for service invocation")]
    public override Task<TResponse> InvokeMethodGrpcAsync<TRequest, TResponse>(string appId, string methodName, TRequest data, CancellationToken cancellationToken = default)
        => throw Unsupported();

    private static NotSupportedException Unsupported()
        => new("This conditional state seam only implements state reads, conditional saves, and conditional deletes.");

    internal readonly record struct IdentitySave(string StoreName, StateOptions? Options, string Etag, bool Saved);
}
