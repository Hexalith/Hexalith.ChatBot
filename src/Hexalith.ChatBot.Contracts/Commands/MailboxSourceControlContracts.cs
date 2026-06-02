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
/// Known schema versions for the FR74/FR75 mailbox-source rate-limit (Story 7.14) command. A dedicated constant —
/// not <see cref="MailboxSourceControlSchemaVersions.V1"/> — because the rate-limit command shape diverges from the
/// disable/quarantine two-person control commands: it is single-actor and carries a bounded budget + window token
/// instead of <c>MailboxSourceControlState</c> old/new-state fields.
/// </summary>
public static class MailboxSourceRateLimitSchemaVersions
{
    public const string V1 = "mailbox-source-rate-limit-schema.v1";

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

/// <summary>
/// Single-actor, schema-bounded proposal to rate-limit a noisy mailbox source (Story 7.14, FR74/FR75). Unlike the
/// security-sensitive disable/quarantine controls, rate-limit is a <em>standard policy mutation</em> per the FR74
/// decomposition guidance: it follows the single-actor <see cref="SubmitMailboxConfigurationChange"/> authorization
/// shape (one <c>HasHumanAdminScope(AdminScope.Mailbox)</c> check) — there is no <c>Approve…</c> counterpart, no
/// distinct-approver guard, and no <see cref="MailboxSourceControlState"/> transition. "Old state"/"new state" are the
/// prior and new per-window budgets, not a control state. Tenant and actor authority are supplied by the authenticated
/// gateway binding, never the command body. Carries only safe, finite, metadata-only tokens — never mailbox
/// subject/body, sender/recipient addresses, or secrets. The budget is bounded by <see cref="MailboxRateLimitBounds"/>.
/// </summary>
public sealed record SubmitMailboxSourceRateLimit(
    string RateLimitChangeId,
    string MailboxSourceRef,
    string ReasonCode,
    string PolicySnapshotId,
    int OldBudget,
    int NewBudget,
    MailboxRateLimitWindow Window,
    long SourceVersion,
    string RequesterRef,
    string SchemaVersion,
    string CorrelationId) : IChatBotCommand;
