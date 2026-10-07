using Hexalith.ChatBot.Client.Generated;
using Hexalith.ChatBot.Server.Gateway;
using Hexalith.ChatBot.Server.Gateway.Idempotency;

namespace Hexalith.ChatBot.Server.Gateway.Stages;

internal interface IIdempotencyStore
{
    ValueTask<CoarseIdempotencyDecision> RecordAdmissionAsync(ChatBotGatewayContext context, CancellationToken cancellationToken);

    /// <summary>Durably prepares the safe outcome and fences dispatch against expired or replaced admission ownership.</summary>
    ValueTask<bool> PrepareDispatchAsync(
        CoarseIdempotencyMetadata metadata,
        CommandSubmissionResponse outcome,
        CancellationToken cancellationToken) => ValueTask.FromResult(false);

    /// <summary>Fences the actual planned EventStore aggregate target onto the matching prepared dispatch ownership.</summary>
    ValueTask<bool> BindDispatchTargetAsync(
        CoarseIdempotencyMetadata metadata,
        CommandSubmissionResponse preparedOutcome,
        string aggregateId,
        CancellationToken cancellationToken) => ValueTask.FromResult(false);

    /// <summary>Retains observed SDK acceptance only for the exact prepared owner, response, and aggregate.</summary>
    ValueTask<bool> ConfirmSdkSubmissionAcceptedAsync(
        CoarseIdempotencyMetadata metadata,
        CommandSubmissionResponse preparedOutcome,
        string aggregateId,
        CancellationToken cancellationToken) => ValueTask.FromResult(false);

    ValueTask RecordOutcomeAsync(
        CoarseIdempotencyMetadata metadata,
        CommandSubmissionResponse outcome,
        CancellationToken cancellationToken);

    ValueTask AbortAdmissionAsync(CoarseIdempotencyMetadata metadata, CancellationToken cancellationToken);

    /// <summary>
    /// Fences matching prepared ownership after dispatch proves nothing committed: no external write was attempted, or
    /// EventStore definitively refused the only one.
    /// </summary>
    ValueTask AbortUndispatchedAsync(CoarseIdempotencyMetadata metadata, CommandSubmissionResponse preparedOutcome,
        CancellationToken cancellationToken) => AbortAdmissionAsync(metadata, cancellationToken);
}
