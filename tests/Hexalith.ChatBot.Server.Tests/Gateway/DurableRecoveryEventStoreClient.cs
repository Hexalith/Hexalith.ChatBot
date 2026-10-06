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

    /// <summary>Gets the message identifier of every submission, in order.</summary>
    public List<string> SubmittedMessageIds { get; } = [];

    public bool SubmissionUncertain { get; init; }

    public CancellationTokenSource? CancelOnSubmission { get; init; }

    /// <summary>Gets or sets a failure EventStore returns for every submission while set (for example a refusal).</summary>
    public Exception? SubmissionFailure { get; set; }

    public Task<CommandStatusQueryResponse?> GetCommandStatusAsync(string messageId, CancellationToken cancellationToken = default)
        => Unavailable ? throw new HttpRequestException("Injected platform outage.") : Task.FromResult(Evidence);

    public Task<SubmitCommandResponse> SubmitCommandAsync(SubmitCommandRequest request, CancellationToken cancellationToken = default)
    {
        SubmissionCount++;
        SubmittedMessageIds.Add(request.MessageId);
        CancelOnSubmission?.Cancel();
        if (SubmissionFailure is { } failure)
        {
            throw failure;
        }

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
