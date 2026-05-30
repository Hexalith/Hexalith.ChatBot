using Hexalith.ChatBot.Client.Generated;

namespace Hexalith.ChatBot.Server.Gateway.Idempotency;

internal sealed record CoarseIdempotencyRecord(
    string TenantId,
    string OperationClass,
    string CoarseKeyHash,
    string CanonicalEquivalenceHash,
    string CorrelationId,
    string? TaskId,
    string CommandId,
    string CommandType,
    string RequesterId,
    DateTimeOffset CreatedAt,
    DateTimeOffset ExpiresAt,
    CommandSubmissionResponse? PriorOutcome);
