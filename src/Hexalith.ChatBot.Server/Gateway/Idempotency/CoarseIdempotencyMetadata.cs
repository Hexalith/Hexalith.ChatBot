namespace Hexalith.ChatBot.Server.Gateway.Idempotency;

internal sealed record CoarseIdempotencyMetadata(
    string OperationClass,
    string CoarseKeyHash,
    string CanonicalEquivalenceHash,
    DateTimeOffset ExpiresAt)
{
    public static CoarseIdempotencyMetadata UnsafeCreateForTesting(
        string operationClass,
        string coarseKeyHash,
        string canonicalEquivalenceHash,
        DateTimeOffset expiresAt)
        => new(operationClass, coarseKeyHash, canonicalEquivalenceHash, expiresAt);
}
