namespace Hexalith.ChatBot.Server.Gateway;

/// <summary>
/// In-process gateway seam that accumulates authorization-failure events per tenant for the NFR43
/// authorization-failure-spike alert (Story 8.4, AC5). It is fed only the bound tenant id, or the literal
/// <c>unavailable</c> when no tenant is bound (never the actor, command, or reason), from the same code path that
/// emits <see cref="ChatBotAuthorizationFailureAuditFact"/>, and exposes a
/// coarse rolling-window count the spike evaluator consumes. Lives alongside the gateway authorization seam.
/// The Story 1.6 <c>policy-snapshot-unavailable</c> dependency denial records the fact without incrementing
/// this counter.
/// </summary>
internal interface IAuthorizationFailureCounter
{
    /// <summary>Records one authorization failure for the given tenant at the supplied server-measured timestamp.</summary>
    void Record(string tenantId, DateTimeOffset timestamp);

    /// <summary>
    /// Prunes events outside the rolling window and returns the current per-tenant aggregate counts. This is a
    /// sliding window (not a tumbling window): in-window events are retained so a sustained spike keeps being
    /// reported across passes.
    /// </summary>
    IReadOnlyList<AuthorizationFailureReading> ReadAndReset();
}
