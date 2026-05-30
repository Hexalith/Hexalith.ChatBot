using Hexalith.ChatBot.Client.Generated;
using Hexalith.ChatBot.Server.Gateway;
using Hexalith.ChatBot.Server.Gateway.Idempotency;

namespace Hexalith.ChatBot.Server.Gateway.Stages;

internal interface IIdempotencyStore
{
    ValueTask<CoarseIdempotencyDecision> RecordAdmissionAsync(ChatBotGatewayContext context, CancellationToken cancellationToken);

    ValueTask RecordOutcomeAsync(
        CoarseIdempotencyMetadata metadata,
        CommandSubmissionResponse outcome,
        CancellationToken cancellationToken);

    ValueTask AbortAdmissionAsync(CoarseIdempotencyMetadata metadata, CancellationToken cancellationToken);
}
