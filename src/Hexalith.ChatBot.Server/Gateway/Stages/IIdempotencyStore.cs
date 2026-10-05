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

    ValueTask RecordOutcomeAsync(
        CoarseIdempotencyMetadata metadata,
        CommandSubmissionResponse outcome,
        CancellationToken cancellationToken);

    ValueTask AbortAdmissionAsync(CoarseIdempotencyMetadata metadata, CancellationToken cancellationToken);

    /// <summary>Fences matching prepared ownership after dispatch proves no external write was attempted.</summary>
    ValueTask AbortUndispatchedAsync(CoarseIdempotencyMetadata metadata, CommandSubmissionResponse preparedOutcome,
        CancellationToken cancellationToken) => AbortAdmissionAsync(metadata, cancellationToken);
}
