using Hexalith.ChatBot.Contracts.Enums;

namespace Hexalith.ChatBot.Contracts.Commands;

/// <summary>
/// Known schema versions for the FR74 mailbox-source control (disable + quarantine) commands.
/// Quarantine (Story 7.13) reuses <see cref="V1"/> — the two-person submit→approve command shape is identical
/// to disable, so a dedicated schema-version constant is not required.
/// </summary>
public static class MailboxSourceControlSchemaVersions
{
    public const string V1 = "mailbox-source-control-schema.v1";

    public static IReadOnlySet<string> All { get; } =
        new HashSet<string>([V1], StringComparer.Ordinal);

    public static bool IsKnown(string? schemaVersion)
        => !string.IsNullOrWhiteSpace(schemaVersion) && All.Contains(schemaVersion);
}

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

/// <summary>
/// Second-person approval that activates a pending mailbox-source disable (FR75d). The approver MUST be a
/// different human from the proposer; this is re-checked in the aggregate as defense-in-depth.
/// </summary>
public sealed record ApproveMailboxSourceDisable(
    string DisableChangeId,
    string MailboxSourceRef,
    string ReasonCode,
    string PolicySnapshotId,
    MailboxSourceControlState OldState,
    MailboxSourceControlState NewState,
    long SourceVersion,
    string RequesterRef,
    string ApproverRef,
    string SchemaVersion,
    string CorrelationId) : IChatBotCommand;

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

/// <summary>
/// Second-person approval that activates a pending mailbox-source quarantine (FR75d). The approver MUST be a
/// different human from the proposer; this is re-checked in the aggregate as defense-in-depth.
/// </summary>
public sealed record ApproveMailboxSourceQuarantine(
    string QuarantineChangeId,
    string MailboxSourceRef,
    string ReasonCode,
    string PolicySnapshotId,
    MailboxSourceControlState OldState,
    MailboxSourceControlState NewState,
    long SourceVersion,
    string RequesterRef,
    string ApproverRef,
    string SchemaVersion,
    string CorrelationId) : IChatBotCommand;
