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

    // Story 9.2 (NFR50a): the per-tenant audit-completeness fraction dropped below the 99.5% rolling-7-day target, OR
    // the measurement could not complete (unmeasurable → breach, never a silent pass). This is a P1 incident — audit
    // completeness is the compliance proof that "complete audit" is measured, not assumed — so the downstream incident
    // routing must treat it at P1 severity (the alert/payload encodes P1 explicitly).
    AuditCompletenessBudgetBreached,
}
