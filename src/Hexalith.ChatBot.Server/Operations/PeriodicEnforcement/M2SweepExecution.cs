namespace Hexalith.ChatBot.Server.Operations.PeriodicEnforcement;

/// <summary>
/// Why an M2 sweep produced no outcome on a given pass. A bare <see langword="null"/> outcome is ambiguous — it cannot
/// distinguish "already swept this period" from "attempted and threw".
/// </summary>
internal enum M2SweepExecution
{
    /// <summary>The sweep did not run on this pass: the cadence partition was already committed, or a failed attempt
    /// is still inside its retry backoff, or M2 sweeps are disabled.</summary>
    Skipped,

    /// <summary>The sweep ran and completed; its outcome is present.</summary>
    Completed,

    /// <summary>The sweep ran and threw. Its partition was NOT committed, so it is retried after the backoff.</summary>
    Failed,
}
