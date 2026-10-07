using Hexalith.EventStore.Client.Gateway;
using Hexalith.EventStore.Contracts.Commands;
using Hexalith.EventStore.Contracts.Queries;
using Hexalith.EventStore.Contracts.Streams;

namespace Hexalith.ChatBot.Server.Tests;

/// <summary>Retains exact SDK submissions for internal producer admission checks.</summary>
internal sealed class TrustedAuthorityRecordingEventStoreClient : IEventStoreGatewayClient
{
    /// <summary>Exact commands submitted to the EventStore client seam.</summary>
    public List<SubmitCommandRequest> Submitted { get; } = [];
    /// <inheritdoc/>
    public Task<SubmitCommandResponse> SubmitCommandAsync(SubmitCommandRequest request, CancellationToken cancellationToken = default)
    {
        Submitted.Add(request);
        return Task.FromResult(new SubmitCommandResponse(request.CorrelationId ?? request.MessageId));
    }
    /// <inheritdoc/>
    public Task<EventStoreQueryResult> SubmitQueryAsync(SubmitQueryRequest request, string? ifNoneMatch = null, CancellationToken cancellationToken = default) => throw new NotSupportedException();
    /// <inheritdoc/>
    public Task<EventStoreQueryResult<T>> SubmitQueryAsync<T>(SubmitQueryRequest request, string? ifNoneMatch = null, CancellationToken cancellationToken = default) => throw new NotSupportedException();
    /// <inheritdoc/>
    public Task<StreamReadPage> ReadStreamAsync(StreamReadRequest request, CancellationToken cancellationToken = default) => throw new NotSupportedException();
}
