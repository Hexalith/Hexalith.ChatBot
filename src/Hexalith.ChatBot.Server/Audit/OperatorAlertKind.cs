namespace Hexalith.ChatBot.Server.Audit;

internal enum OperatorAlertKind
{
    AuditUnavailable,
    TenantScopeUnresolved,
    PostCommitAuditReconciliationRequired,
    CorrectionDelayed,
    RetryExhausted,
    DependencyDegraded,

    // Story 8.4 (NFR43): the four additional default operational alert thresholds wired alongside the existing
    // RetryExhausted kind (which already matches AC2's retry-exhaustion firing condition).
    AuditProjectionLagBreached,
    SubscriptionExpiryImminent,
    ApprovalQueueAgeBreached,
    AuthorizationFailureSpike,

    // Story 9.1 (NFR49a): the nightly per-tenant WORM chain verification detected a broken/incomplete chain. A
    // metadata-only operator alert to the on-call security engineer, fail-closed (an incomplete verification is itself
    // a breach signal, never a silent pass).
    AuditChainBroken,
}
