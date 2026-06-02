namespace Hexalith.ChatBot.Contracts.Commands;

/// <summary>
/// Governed mailbox configuration change submission. Tenant and human mailbox authority are supplied by the gateway.
/// </summary>
public sealed record SubmitMailboxConfigurationChange(
    string ConfigurationChangeId,
    string SourceConfigurationSnapshotId,
    string ProposedConfigurationSnapshotId,
    long SourceVersion,
    MailboxConfigurationChangeSet ChangeSet,
    string ReasonCode,
    string RequesterRef,
    string SchemaVersion,
    string CorrelationId,
    string OldConfigurationFingerprint,
    string NewConfigurationFingerprint) : IChatBotCommand;
