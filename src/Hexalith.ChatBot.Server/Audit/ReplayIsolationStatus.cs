namespace Hexalith.ChatBot.Server.Audit;

/// <summary>The outcome of verifying a production tenant's replay isolation (Story 9.4, FR95a).</summary>
internal enum ReplayIsolationStatus
{
    /// <summary>No replay-marked record exists in the production tenant's outbound-trace store or WORM chain.</summary>
    Clean,

    /// <summary>A replay-marked record was found in a production tenant — a stop-ship / M2-gating isolation breach.</summary>
    Breach,

    /// <summary>Verification could not complete (store enumeration threw). Treated as a breach signal, never a silent pass.</summary>
    Unknown,
}
