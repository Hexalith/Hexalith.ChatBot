using Hexalith.ChatBot.Contracts.Enums;

namespace Hexalith.ChatBot.Contracts.Commands;

public sealed record MailboxProviderConnectionMetadata(
    string ProviderConnectionRef,
    MailboxProviderKind ProviderKind,
    string CredentialFingerprint,
    string PermissionEvidenceRef,
    MailboxPermissionFreshnessState Freshness,
    DateTimeOffset LastCheckedAt);
