namespace Hexalith.ChatBot.Server.Observability;

/// <summary>
/// In-process seam the operational alert wiring coordinator reads to learn which tenants reached the retry-exhausted
/// terminal state since the previous evaluation pass (Story 8.4, AC2). It sits alongside the
/// <see cref="IChatBotMetrics.RecordRetryExhausted"/> path: the concrete <see cref="ChatBotMetrics"/> implementation
/// calls <see cref="Signal"/> after the OTel counter increment (non-throwing, fire-and-forget). The default
/// production implementation (<see cref="InMemoryRetryExhaustionAlertSource"/>) is a thread-safe in-process tenant
/// set, mirroring the <see cref="IAuditProjectionLagSource"/> registration pattern.
/// </summary>
internal interface IRetryExhaustionAlertSource
{
    /// <summary>Records that a retry-exhausted terminal state was reached for the given tenant.</summary>
    void Signal(string tenantId);

    /// <summary>
    /// Reads and clears the retry-exhaustion flag for a single tenant, returning whether exhaustion was signalled for
    /// that tenant since the last call. Per-tenant clearing keeps concurrent per-tenant passes from consuming each
    /// other's signals.
    /// </summary>
    bool ReadAndClear(string tenantId);
}
