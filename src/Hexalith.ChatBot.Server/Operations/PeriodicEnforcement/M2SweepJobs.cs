namespace Hexalith.ChatBot.Server.Operations.PeriodicEnforcement;

/// <summary>
/// The canonical M2 sweep job names. They key the cadence gates, the status store, the per-job reason codes and the
/// release-gate view, so they are declared once here rather than repeated as literals at each call site.
/// </summary>
internal static class M2SweepJobs
{
    public const string WormAuditChain = "worm-audit-chain";

    public const string ReplayIsolationProbe = "replay-isolation-probe";

    public const string DerivedStoreIsolationProbe = "derived-store-isolation-probe";

    /// <summary>Every sweep a release gate expects evidence from. Absence of a name here is itself a stop-ship state.</summary>
    public static readonly string[] All = [WormAuditChain, ReplayIsolationProbe, DerivedStoreIsolationProbe];
}
