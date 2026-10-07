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

    /// <summary>Gets every attempted SDK request, including a transport failure before arrival.</summary>
    public List<SubmitCommandRequest> SubmittedRequests { get; } = [];

    /// <summary>Gets the number of submissions that arrived beyond the injected transport-failure boundary.</summary>
    public int ReceivedSubmissionCount { get; private set; }

    /// <summary>Gets or sets whether an arriving submission retains its genuine aggregate-bound committed status.</summary>
    public bool RetainCommittedEvidence { get; set; }

    /// <summary>Gets or sets a callback invoked once a submission reaches the platform.</summary>
    public Action<SubmitCommandRequest>? OnReceivedSubmission { get; set; }

    public bool SubmissionUncertain { get; set; }

    public CancellationTokenSource? CancelOnSubmission { get; init; }

    /// <summary>Gets or sets a failure EventStore returns for every submission while set (for example a refusal).</summary>
    public Exception? SubmissionFailure { get; set; }

    public Task<CommandStatusQueryResponse?> GetCommandStatusAsync(string messageId, CancellationToken cancellationToken = default)
        => Unavailable ? throw new HttpRequestException("Injected platform outage.") : Task.FromResult(Evidence);

    public Task<SubmitCommandResponse> SubmitCommandAsync(SubmitCommandRequest request, CancellationToken cancellationToken = default)
    {
        SubmissionCount++;
        SubmittedMessageIds.Add(request.MessageId);
        SubmittedRequests.Add(request);
        CancelOnSubmission?.Cancel();
        if (SubmissionFailure is { } failure)
        {
            throw failure;
        }

        cancellationToken.ThrowIfCancellationRequested();
        ReceivedSubmissionCount++;
        if (RetainCommittedEvidence)
        {
            Evidence = new CommandStatusQueryResponse(request.CorrelationId!, nameof(CommandStatus.Completed),
                (int)CommandStatus.Completed, MessageId: request.MessageId)
            {
                TenantId = request.Tenant, Domain = request.Domain, AggregateId = request.AggregateId,
                CommittedEventSequence = 1, EventCount = 1,
            };
        }
        OnReceivedSubmission?.Invoke(request);
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
