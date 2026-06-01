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
}
