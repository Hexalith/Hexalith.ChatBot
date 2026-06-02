using Hexalith.ChatBot.Contracts.Enums;

namespace Hexalith.ChatBot.Contracts.Commands;

/// <summary>
/// Governed mailbox provider connection metadata update. The provider secret remains outside ChatBot.
/// </summary>
public sealed record RecordMailboxProviderConnection(
    string ProviderConnectionChangeId,
    string ProviderConnectionRef,
    MailboxProviderKind ProviderKind,
    string CredentialFingerprint,
    string PermissionEvidenceRef,
    MailboxPermissionFreshnessState Freshness,
    string ReasonCode,
    string RequesterRef,
    long SourceVersion,
    string SchemaVersion,
    string CorrelationId,
    string PolicySnapshotId) : IChatBotCommand;
