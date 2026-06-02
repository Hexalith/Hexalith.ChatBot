namespace Hexalith.ChatBot.Contracts.Commands;

/// <summary>
/// Second-person approval for a pending sensitive tenant policy change.
/// </summary>
public sealed record ApproveTenantPolicyChange(
    string PolicyChangeId,
    string PendingPolicySnapshotId,
    string ActivatedPolicySnapshotId,
    long SourceVersion,
    IReadOnlyList<string> ChangedKnobIds,
    string ReasonCode,
    string RequesterRef,
    string ApproverRef,
    string SchemaVersion,
    string CorrelationId) : IChatBotCommand;
