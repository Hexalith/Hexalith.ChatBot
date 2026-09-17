using Hexalith.ChatBot.Contracts.Enums;

namespace Hexalith.ChatBot.Contracts.Commands;

public static class ComplianceRetentionClassIds
{
    public const string SourceEmailMetadata = "source-email-metadata";
    public const string Attachments = "attachments";
    public const string AssociationRecords = "association-records";
    public const string EvidenceSnapshots = "evidence-snapshots";
    public const string ApprovalRecords = "approval-records";
    public const string PolicySnapshots = "policy-snapshots";
    public const string LifecycleState = "lifecycle-state";
    public const string WorkflowLinkMaps = "workflow-link-maps";
    public const string AiPromptsOutputsContext = "ai-prompts-outputs-context";
    public const string LogsSupportBundles = "logs-support-bundles";
    public const string AuditRecords = "audit-records";

    // Story 9.7 (NFR53): the data-class inventory must distinguish backups and evaluation datasets, which the
    // Story 7.4 retention spine lacked. They are added to the ONE canonical class set both the retention-window
    // editor and the data-class inventory consume — never a parallel DataClassIds enum.
    public const string Backups = "backups";
    public const string EvaluationDatasets = "evaluation-datasets";

    public static IReadOnlySet<string> All { get; } =
        new HashSet<string>(
            [
                SourceEmailMetadata,
                Attachments,
                AssociationRecords,
                EvidenceSnapshots,
                ApprovalRecords,
                PolicySnapshots,
                LifecycleState,
                WorkflowLinkMaps,
                AiPromptsOutputsContext,
                LogsSupportBundles,
                AuditRecords,
                Backups,
                EvaluationDatasets,
            ],
            StringComparer.Ordinal);
}
