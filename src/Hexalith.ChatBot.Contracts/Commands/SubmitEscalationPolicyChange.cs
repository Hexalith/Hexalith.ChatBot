namespace Hexalith.ChatBot.Contracts.Commands;

/// <summary>
/// Governed escalation-policy change submission. Tenant and human admin authority are supplied by the gateway,
/// never the payload. Mirrors <see cref="SubmitNotificationRoutingChange"/>.
/// </summary>
public sealed record SubmitEscalationPolicyChange(
    string EscalationPolicyChangeId,
    string SourceEscalationSnapshotId,
    string ProposedEscalationSnapshotId,
    long SourceVersion,
    EscalationPolicyChangeSet ChangeSet,
    string ReasonCode,
    string RequesterRef,
    string SchemaVersion,
    string CorrelationId,
    string OldEscalationFingerprint,
    string NewEscalationFingerprint) : IChatBotCommand;
