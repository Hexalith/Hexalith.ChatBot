using Hexalith.ChatBot.Client.Generated;

namespace Hexalith.ChatBot.Server.Gateway.Idempotency;

internal enum CoarseIdempotencyDecisionKind
{
    Proceed,
    ReplayPriorOutcome,
    Conflict,
}

internal sealed record CoarseIdempotencyDecision(
    CoarseIdempotencyDecisionKind Kind,
    CoarseIdempotencyMetadata Metadata,
    CommandSubmissionResponse? PriorOutcome)
{
    public static CoarseIdempotencyDecision Proceed(CoarseIdempotencyMetadata metadata)
        => new(CoarseIdempotencyDecisionKind.Proceed, metadata, null);

    public static CoarseIdempotencyDecision ReplayPriorOutcome(
        CoarseIdempotencyMetadata metadata,
        CommandSubmissionResponse priorOutcome)
        => new(CoarseIdempotencyDecisionKind.ReplayPriorOutcome, metadata, priorOutcome);

    public static CoarseIdempotencyDecision Conflict(CoarseIdempotencyMetadata metadata)
        => new(CoarseIdempotencyDecisionKind.Conflict, metadata, null);
}
