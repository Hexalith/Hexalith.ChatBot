namespace Hexalith.ChatBot.Server.Audit;

/// <summary>The outcome of verifying a tenant's WORM audit chain (Story 9.1, NFR49a).</summary>
internal enum WormChainVerificationStatus
{
    /// <summary>Every record's hash, predecessor linkage, and sequence continuity recomputed correctly.</summary>
    Verified,

    /// <summary>A record's recomputed hash, predecessor link, or sequence did not match — the chain is tampered.</summary>
    Broken,

    /// <summary>Verification could not complete (store unavailable / enumeration threw). Treated as a breach signal.</summary>
    Unknown,
}

/// <summary>
/// The metadata-only result of a per-tenant chain verification pass. Carries the status, a bounded reason code, and a
/// safe locator token for the first detected break — never any envelope content. <see cref="IsBreach"/> folds the
/// fail-closed doctrine: anything other than <see cref="WormChainVerificationStatus.Verified"/> is a breach to be
/// alerted (a verification that cannot complete is never a silent success).
/// </summary>
internal sealed record WormAuditChainVerificationResult(
    string TenantRef,
    WormChainVerificationStatus Status,
    string ReasonCode,
    string? FirstBreakLocator)
{
    public bool IsBreach => Status != WormChainVerificationStatus.Verified;

    public const string VerifiedReasonCode = "worm_chain_verified";
    public const string RecordHashMismatchReasonCode = "worm_record_hash_mismatch";
    public const string PredecessorLinkBrokenReasonCode = "worm_predecessor_link_broken";
    public const string SequenceDiscontinuityReasonCode = "worm_sequence_discontinuity";
    public const string VerificationIncompleteReasonCode = "worm_verification_incomplete";
}
