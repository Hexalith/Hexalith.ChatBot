using Hexalith.EventStore.Client.Gateway;
using Hexalith.EventStore.Contracts.Commands;
using Hexalith.EventStore.Contracts.Queries;
using Hexalith.EventStore.Contracts.Streams;

namespace Hexalith.ChatBot.Server.Tests.Gateway;

/// <summary>Retains only authoritative platform evidence across application-service replacement.</summary>
internal sealed class DurableRecoveryEventStoreClient : IEventStoreGatewayClient
{
    public CommandStatusQueryResponse? Evidence { get; set; }

    public bool Unavailable { get; set; }

    public int SubmissionCount { get; private set; }

    public bool SubmissionUncertain { get; init; }

    public CancellationTokenSource? CancelOnSubmission { get; init; }

    public Task<CommandStatusQueryResponse?> GetCommandStatusAsync(string messageId, CancellationToken cancellationToken = default)
        => Unavailable ? throw new HttpRequestException("Injected platform outage.") : Task.FromResult(Evidence);

    public Task<SubmitCommandResponse> SubmitCommandAsync(SubmitCommandRequest request, CancellationToken cancellationToken = default)
    {
        SubmissionCount++;
        CancelOnSubmission?.Cancel();
        cancellationToken.ThrowIfCancellationRequested();
        return SubmissionUncertain ? throw new InvalidOperationException("Injected uncertain platform acknowledgement.")
            : Task.FromResult(new SubmitCommandResponse(request.CorrelationId!, MessageId: request.MessageId));
    }

    public Task<EventStoreQueryResult> SubmitQueryAsync(SubmitQueryRequest request, string? ifNoneMatch = null, CancellationToken cancellationToken = default)
        => throw new NotSupportedException();

    public Task<EventStoreQueryResult<T>> SubmitQueryAsync<T>(SubmitQueryRequest request, string? ifNoneMatch = null, CancellationToken cancellationToken = default)
        => throw new NotSupportedException();

    public Task<StreamReadPage> ReadStreamAsync(StreamReadRequest request, CancellationToken cancellationToken = default)
        => throw new NotSupportedException();
}
