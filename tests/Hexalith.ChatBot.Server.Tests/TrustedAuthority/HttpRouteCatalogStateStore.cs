using System.Text.Json;
using Hexalith.EventStore.ServiceDefaults.Routing;

namespace Hexalith.ChatBot.Server.Tests.TrustedAuthority;

/// <summary>Isolated serialized fixture authority with the SDK optimistic concurrency contract.</summary>
internal sealed class HttpRouteCatalogStateStore : IRouteCatalogStateStore
{
    private readonly object _gate = new();
    private byte[] _state = JsonSerializer.SerializeToUtf8Bytes(RouteCatalogActivationState.Empty);
    private long _version;

    /// <inheritdoc/>
    public RouteCatalogStateCoordinates Coordinates { get; } = new("chatbot-http-fixture-catalog", "authority", "http://127.0.0.1:3500/");

    /// <inheritdoc/>
    public Task<(RouteCatalogActivationState State, string ETag)> ReadAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        lock (_gate)
        {
            return Task.FromResult((JsonSerializer.Deserialize<RouteCatalogActivationState>(_state)!,
                _version.ToString(System.Globalization.CultureInfo.InvariantCulture)));
        }
    }

    /// <inheritdoc/>
    public Task<bool> CompareExchangeAsync(RouteCatalogActivationState state, string etag, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        lock (_gate)
        {
            if (etag != _version.ToString(System.Globalization.CultureInfo.InvariantCulture)) { return Task.FromResult(false); }
            _state = JsonSerializer.SerializeToUtf8Bytes(state);
            _version++;
            return Task.FromResult(true);
        }
    }
}
