using Hexalith.ChatBot.Contracts.Enums;

namespace Hexalith.ChatBot.Contracts.Commands;

/// <summary>
/// First-person proposal to quarantine a mailbox source under the FR75d two-person rule (Story 7.13). Quarantine
/// is a security-sensitive FR74 governance control — not the Story 7.3 mailbox-configuration path: new intake from
/// the source is contained for review while existing records stay auditable. Tenant and actor authority come from
/// the authenticated gateway binding, never the command body. Carries only safe, finite, metadata-only tokens —
/// never mailbox subject/body, sender/recipient addresses, or secrets.
/// </summary>
public sealed record SubmitMailboxSourceQuarantine(
    string QuarantineChangeId,
    string MailboxSourceRef,
    string ReasonCode,
    string PolicySnapshotId,
    MailboxSourceControlState OldState,
    MailboxSourceControlState NewState,
    long SourceVersion,
    string RequesterRef,
    string SchemaVersion,
    string CorrelationId) : IChatBotCommand;
