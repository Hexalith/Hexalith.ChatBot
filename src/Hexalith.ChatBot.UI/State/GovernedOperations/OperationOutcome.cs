namespace Hexalith.ChatBot.UI.State.GovernedOperations;

/// <summary>
/// Metadata-only view of a governed operation outcome rendered by the UI. Carries identifiers and stable
/// status codes only — never command payload, tenant/resource names, secrets, or raw text. The completion
/// status is the freshness-honest value read back from the operation status (never a premature "Done").
/// </summary>
/// <param name="OperationId">The operation identity used to read status.</param>
/// <param name="CommandId">The submitted command identity.</param>
/// <param name="CorrelationId">The correlation identity carried through the spine.</param>
/// <param name="LifecycleState">The lifecycle state code.</param>
/// <param name="CompletionStatus">The freshness-honest completion status code.</param>
/// <param name="AuditStatus">The post-commit audit status code.</param>
/// <param name="SafeNextActions">The safe next-action codes.</param>
/// <param name="AuditHistory">The metadata-only audit-history summary lines.</param>
/// <param name="RetryCount">The retry attempt count exposed by operation status.</param>
/// <param name="OperationClass">The metadata-only operation class.</param>
/// <param name="OwnerRole">The owner role responsible for recovery when present.</param>
/// <param name="DuplicateSafetyNote">The duplicate-safety note when present.</param>
public sealed record OperationOutcome(
    string OperationId,
    string CommandId,
    string CorrelationId,
    string LifecycleState,
    string CompletionStatus,
    string AuditStatus,
    IReadOnlyList<string> SafeNextActions,
    IReadOnlyList<string> AuditHistory,
    int RetryCount = 0,
    string OperationClass = "command-execution",
    string? OwnerRole = null,
    string? DuplicateSafetyNote = null);
