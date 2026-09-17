namespace Hexalith.ChatBot.Server.Audit;

/// <summary>
/// The metadata-only result of a per-tenant replay-isolation verification pass. Carries the status, a bounded reason
/// code, and a safe locator token for the first offending record — never any record content. <see cref="IsBreach"/>
/// folds the fail-closed doctrine: anything other than <see cref="ReplayIsolationStatus.Clean"/> is a breach to be
/// alerted (a sweep that cannot complete is never a silent pass). Mirrors <see cref="WormAuditChainVerificationResult"/>.
/// </summary>
internal sealed record ReplayIsolationVerificationResult(
    string TenantRef,
    ReplayIsolationStatus Status,
    string ReasonCode,
    string? FirstOffenderLocator)
{
    public bool IsBreach => Status != ReplayIsolationStatus.Clean;

    public const string CleanReasonCode = "replay_isolation_clean";
    public const string TraceBreachReasonCode = "replay_isolation_trace_breach";
    public const string ChainBreachReasonCode = "replay_isolation_chain_breach";
    public const string SweepIncompleteReasonCode = "replay_isolation_sweep_incomplete";
}
