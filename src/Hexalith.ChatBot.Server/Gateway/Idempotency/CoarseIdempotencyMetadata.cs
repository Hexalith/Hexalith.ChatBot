namespace Hexalith.ChatBot.Server.Gateway.Idempotency;

internal sealed record CoarseIdempotencyMetadata(
    string OperationClass,
    string CoarseKeyHash,
    string CanonicalEquivalenceHash,
    DateTimeOffset ExpiresAt,
    string? IdentityKeyHash = null,
    DateTimeOffset? CreatedAt = null,
    string? ReservationId = null)
{
    public static CoarseIdempotencyMetadata UnsafeCreateForTesting(
        string operationClass,
        string coarseKeyHash,
        string canonicalEquivalenceHash,
        DateTimeOffset expiresAt)
        => new(operationClass, coarseKeyHash, canonicalEquivalenceHash, expiresAt);
}
