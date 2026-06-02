namespace Hexalith.ChatBot.Contracts.Commands;

/// <summary>
/// Governed notification-routing change submission. Tenant and human admin authority are supplied by the gateway,
/// never the payload. Mirrors <see cref="SubmitMailboxConfigurationChange"/> / <see cref="SubmitTenantPolicyChange"/>.
/// </summary>
public sealed record SubmitNotificationRoutingChange(
    string RoutingChangeId,
    string SourceRoutingSnapshotId,
    string ProposedRoutingSnapshotId,
    long SourceVersion,
    NotificationRoutingChangeSet ChangeSet,
    string ReasonCode,
    string RequesterRef,
    string SchemaVersion,
    string CorrelationId,
    string OldRoutingFingerprint,
    string NewRoutingFingerprint) : IChatBotCommand;
