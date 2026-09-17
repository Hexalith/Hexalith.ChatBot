using Hexalith.ChatBot.Contracts.Enums;

namespace Hexalith.ChatBot.Contracts.Commands;

public sealed record MailboxPermissionStatus(
    string PermissionStatusRef,
    string ProviderConnectionRef,
    string Permission,
    MailboxPermissionFreshnessState Freshness,
    string PermissionEvidenceRef,
    DateTimeOffset LastCheckedAt,
    string ReasonCode);
