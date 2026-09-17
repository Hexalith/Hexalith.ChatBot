namespace Hexalith.ChatBot.Server.Audit;

/// <summary>The outcome of an active cross-tenant read attempt against a tenant pair's derived stores (Story 9.5, FR55a).</summary>
internal enum DerivedStoreIsolationStatus
{
    /// <summary>The intruder tenant observed none of the owner tenant's seeded sentinels — isolation held.</summary>
    Clean,

    /// <summary>The intruder observed at least one of the owner's sentinels — a stop-ship / M2-gating isolation breach.</summary>
    Breach,

    /// <summary>The probe could not complete (the store seam threw during seed or read-back). A breach signal, never a silent pass.</summary>
    Unknown,
}
