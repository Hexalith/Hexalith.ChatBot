using Hexalith.ChatBot.Client.Generated;

namespace Hexalith.ChatBot.Server.Gateway.Idempotency;

/// <summary>
/// Tenant-scoped caller identity retained after a short domain replay window expires.
/// Its embedded domain reservation is the durable lease/dispatch fence and retains the exact prepared safe outcome
/// and the aggregate target selected by the dispatch plan, with positive SDK submission acceptance when observed.
/// </summary>
internal sealed record CoarseCommandIdentityRecord(
    string TenantId,
    string CommandId,
    string CallerFingerprint,
    string DomainKeyHash,
    DateTimeOffset CreatedAt,
    CommandSubmissionResponse? PriorOutcome,
    CoarseIdempotencyRecord? DomainReservation = null,
    bool Released = false);
