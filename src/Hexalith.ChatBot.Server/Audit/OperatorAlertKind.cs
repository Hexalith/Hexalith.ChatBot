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
}
