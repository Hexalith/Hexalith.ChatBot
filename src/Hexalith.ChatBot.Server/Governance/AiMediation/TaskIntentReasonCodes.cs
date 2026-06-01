namespace Hexalith.ChatBot.Server.Governance.AiMediation;

internal static class TaskIntentReasonCodes
{
    public const string Captured = "task_intent_captured";
    public const string NotActionable = "task_intent_not_actionable";
    public const string MissingTenantScope = "task_intent_tenant_scope_unresolved";
    public const string MissingProjectAuthorization = "task_intent_project_authorization_unresolved";
    public const string MissingSourceAuthorization = "task_intent_source_authorization_unresolved";
    public const string MissingRequesterParty = "task_intent_requester_party_unresolved";
    public const string MissingAuditReadiness = "task_intent_audit_not_ready";
    public const string RedactedSource = "task_intent_source_redacted";
    public const string StaleCorrectedContext = "task_intent_corrected_context_stale";
    public const string InvalidConfidence = "task_intent_confidence_invalid";
    public const string MissingSourceEvidence = "task_intent_source_evidence_unresolved";
    public const string Converted = "task_intent_converted";
    public const string DispositionMarked = "task_intent_disposition_marked";
    public const string MissingCapturedIntent = "task_intent_unavailable";
    public const string SourceVersionMismatch = "task_intent_source_version_mismatch";
    public const string TerminalState = "task_intent_terminal_state";
    public const string AlreadyConverted = "task_intent_already_converted";
    public const string DuplicatePredecessorInvalid = "task_intent_duplicate_predecessor_unavailable";
    public const string UnsupportedTransition = "task_intent_transition_unsupported";
    public const string InvalidMetadata = "task_intent_transition_metadata_invalid";
    public const string IdempotencyConflict = "task_intent_transition_idempotency_conflict";
    public const string SourceUnavailable = "task_intent_source_unavailable";
    public const string PolicyBlocked = "task_intent_policy_blocked";
}
