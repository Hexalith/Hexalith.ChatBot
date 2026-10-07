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
    CommandSubmissionResponse? PriorOutcome,
    string? IdentityKeyHash = null,
    string? CallerFingerprint = null,
    string? LegacyKeyHash = null,
    string? ReservationId = null,
    CoarseDispatchState DispatchState = CoarseDispatchState.Unknown,
    DateTimeOffset? ReservationLeaseExpiresAt = null,
    CommandSubmissionResponse? PreparedOutcome = null,
    bool Released = false,
    string? PreparedAggregateId = null,
    bool SdkSubmissionAccepted = false);
