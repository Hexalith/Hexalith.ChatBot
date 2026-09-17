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
