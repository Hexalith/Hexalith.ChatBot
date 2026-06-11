namespace Hexalith.ChatBot.Server.Lifecycle.Workflows;

internal static class CorrectionPropagationWorkflowFailureCodes
{
    public const string None = "none";
    public const string WorkflowUnavailable = "association_correction_workflow_unavailable";
    public const string StateStoreUnavailable = "association_correction_state_store_unavailable";
    public const string PubSubUnavailable = "association_correction_pubsub_unavailable";
    public const string AuditUnavailable = "association_correction_audit_unavailable";
    public const string ProjectionUnavailable = "association_correction_projection_unavailable";
    public const string EventStoreWriterUnavailable = "association_correction_eventstore_writer_unavailable";
    public const string StoreUnavailable = "association_correction_store_unavailable";
}
