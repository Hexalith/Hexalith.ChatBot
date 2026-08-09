namespace Hexalith.ChatBot.Server.Lifecycle.Workflows;

internal static class CorrectionPropagationWorkflowFailureCodes
{
    public const string None = "none";
    public const string WorkflowUnavailable = "association_correction_workflow_unavailable";
    public const string AuditUnavailable = "association_correction_audit_unavailable";
    public const string StoreUnavailable = "association_correction_store_unavailable";
}
