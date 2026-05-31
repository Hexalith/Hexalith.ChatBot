namespace Hexalith.ChatBot.Server.Audit;

internal enum OperatorAlertKind
{
    AuditUnavailable,
    TenantScopeUnresolved,
    PostCommitAuditReconciliationRequired,
    CorrectionDelayed,
    RetryExhausted,
    DependencyDegraded,
}
