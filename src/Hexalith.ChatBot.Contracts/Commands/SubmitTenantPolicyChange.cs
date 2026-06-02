namespace Hexalith.ChatBot.Contracts.Commands;

/// <summary>
/// Governed tenant policy change submission. Tenant and actor authority are supplied by the gateway.
/// </summary>
public sealed record SubmitTenantPolicyChange(
    string PolicyChangeId,
    string SourcePolicySnapshotId,
    string ProposedPolicySnapshotId,
    long SourceVersion,
    IReadOnlyList<string> ChangedKnobIds,
    TenantPolicyChangeSet ChangeSet,
    string ReasonCode,
    string RequesterRef,
    string SchemaVersion,
    string CorrelationId,
    string OldValueFingerprint,
    string NewValueFingerprint) : IChatBotCommand;
