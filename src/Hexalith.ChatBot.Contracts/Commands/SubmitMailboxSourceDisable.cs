using Hexalith.ChatBot.Contracts.Enums;

namespace Hexalith.ChatBot.Contracts.Commands;

/// <summary>
/// First-person proposal to disable a mailbox source under the FR75d two-person rule. Tenant and actor
/// authority are supplied by the authenticated gateway binding, never the command body. Carries only safe,
/// finite, metadata-only tokens — never mailbox subject/body, sender/recipient addresses, or secrets.
/// </summary>
public sealed record SubmitMailboxSourceDisable(
    string DisableChangeId,
    string MailboxSourceRef,
    string ReasonCode,
    string PolicySnapshotId,
    MailboxSourceControlState OldState,
    MailboxSourceControlState NewState,
    long SourceVersion,
    string RequesterRef,
    string SchemaVersion,
    string CorrelationId) : IChatBotCommand;
