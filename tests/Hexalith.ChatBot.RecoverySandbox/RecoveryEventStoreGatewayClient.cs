using Hexalith.EventStore.Client.Gateway;
using Hexalith.EventStore.Contracts.Commands;
using Hexalith.EventStore.Contracts.Queries;
using Hexalith.EventStore.Contracts.Streams;

namespace Hexalith.ChatBot.RecoverySandbox;

/// <summary>Controllable command client behind the real <c>AcceptedCommandDispatcher</c> boundary.</summary>
internal sealed class RecoveryEventStoreGatewayClient(RecoveryScopedOutageState state) : IEventStoreGatewayClient
{
    /// <summary>The most recent command the real dispatcher actually submitted, for post-dispatch effect inspection.</summary>
    public SubmitCommandRequest? LastSubmitted { get; private set; }

    /// <summary>
    /// When <see langword="false"/>, successful submits are not recorded on the command-execution effect ledger
    /// (used by the ai-provider exercise, which already records on its own dependency token).
    /// </summary>
    public bool RecordCommandExecutionEffects { get; set; } = true;

    /// <inheritdoc />
    public Task<SubmitCommandResponse> SubmitCommandAsync(
        SubmitCommandRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        cancellationToken.ThrowIfCancellationRequested();
        if (state.IsFaulted("command-execution"))
        {
            state.RecordFaultObservation("command-execution");
            throw new HttpRequestException("The recovery command-execution boundary is unavailable.");
        }

        LastSubmitted = request;
        if (RecordCommandExecutionEffects)
        {
            _ = state.RecordEffect("command-execution", request.Tenant, request.MessageId);
        }

        return Task.FromResult(new SubmitCommandResponse(request.CorrelationId ?? request.MessageId, MessageId: request.MessageId));
    }

    /// <inheritdoc />
    public Task<EventStoreQueryResult> SubmitQueryAsync(
        SubmitQueryRequest request,
        string? ifNoneMatch = null,
        CancellationToken cancellationToken = default)
        => throw new NotSupportedException("The recovery command exercise does not issue queries.");

    /// <inheritdoc />
    public Task<EventStoreQueryResult<T>> SubmitQueryAsync<T>(
        SubmitQueryRequest request,
        string? ifNoneMatch = null,
        CancellationToken cancellationToken = default)
        => throw new NotSupportedException("The recovery command exercise does not issue queries.");

    /// <inheritdoc />
    public Task<StreamReadPage> ReadStreamAsync(
        StreamReadRequest request,
        CancellationToken cancellationToken = default)
        => throw new NotSupportedException("The recovery command exercise does not read streams.");
}
