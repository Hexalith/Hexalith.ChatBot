using Hexalith.ChatBot.Contracts.Enums;

namespace Hexalith.ChatBot.Contracts.Commands;

/// <summary>
/// Known schema versions for the FR74 outbound-channel control (disable) commands. A dedicated constant — not
/// <see cref="CommandCapabilityControlSchemaVersions.V1"/> — because the subject diverges from the command-capability
/// control plane: the outbound channel is identified by its safe channel ref (<c>OutboundChannelRef</c> — the
/// <c>AdapterRef</c> token, e.g. <c>adapter:mailbox-outbound</c>), not a command type name or actor id. Mirrors
/// <see cref="CommandCapabilityControlSchemaVersions"/>.
/// </summary>
public static class OutboundChannelControlSchemaVersions
{
    public const string V1 = "outbound-channel-control-schema.v1";

    public static IReadOnlySet<string> All { get; } =
        new HashSet<string>([V1], StringComparer.Ordinal);

    public static bool IsKnown(string? schemaVersion)
        => !string.IsNullOrWhiteSpace(schemaVersion) && All.Contains(schemaVersion);
}

/// <summary>
/// First-person proposal to disable an outbound channel under the FR75d two-person rule — its future approved sends
/// are blocked for the tenant and fail closed at the outbound send seam (before the external adapter call) until the
/// channel is re-enabled. Pending drafts/approvals and all existing records stay inspectable. Tenant authority is
/// supplied by the authenticated gateway binding, never the command body. Carries only safe, finite, metadata-only
/// tokens — never recipient/sender addresses, message content, credentials, OAuth grant fingerprints, model
/// prompts/completions, or delegated-user PII. The subject is identified by its safe channel ref (the
/// <see cref="OutboundChannelRef"/>), a finite stable identifier. Mirrors <see cref="SubmitCommandCapabilityDisable"/>
/// (Story 7.21), retargeted from the command-type subject to the outbound-channel subject.
/// </summary>
public sealed record SubmitOutboundChannelDisable(
    string DisableChangeId,
    string OutboundChannelRef,
    string ReasonCode,
    string PolicySnapshotId,
    OutboundChannelControlState OldState,
    OutboundChannelControlState NewState,
    long SourceVersion,
    string RequesterRef,
    string SchemaVersion,
    string CorrelationId) : IChatBotCommand;

/// <summary>
/// Second-person approval that activates a pending outbound-channel disable (FR75d). The approver MUST be a different
/// human from the proposer; this is re-checked in the aggregate as defense-in-depth. Mirrors
/// <see cref="ApproveCommandCapabilityDisable"/>.
/// </summary>
public sealed record ApproveOutboundChannelDisable(
    string DisableChangeId,
    string OutboundChannelRef,
    string ReasonCode,
    string PolicySnapshotId,
    OutboundChannelControlState OldState,
    OutboundChannelControlState NewState,
    long SourceVersion,
    string RequesterRef,
    string ApproverRef,
    string SchemaVersion,
    string CorrelationId) : IChatBotCommand;

/// <summary>
/// First-person proposal to quarantine an outbound channel under the FR75d two-person rule — its future approved
/// sends are held for manual review for the tenant and fail closed at the outbound send seam (before the external
/// adapter call) until a policy administrator reviews and releases it. Pending drafts/approvals and all existing
/// records stay inspectable. Tenant authority is supplied by the authenticated gateway binding, never the command
/// body. Carries only safe, finite, metadata-only tokens — never recipient/sender addresses, message content,
/// credentials, OAuth grant fingerprints, model prompts/completions, or delegated-user PII. The subject is identified
/// by its safe channel ref (the <see cref="OutboundChannelRef"/>), a finite stable identifier. Mirrors
/// <see cref="SubmitOutboundChannelDisable"/> (Story 7.24), substituting <c>Quarantine</c>/<c>Quarantined</c> for
/// <c>Disable</c>/<c>Disabled</c> and reusing <see cref="OutboundChannelControlSchemaVersions.V1"/> — the command
/// shape is identical to disable (the Story 7.22 disable→quarantine substitution precedent).
/// </summary>
public sealed record SubmitOutboundChannelQuarantine(
    string QuarantineChangeId,
    string OutboundChannelRef,
    string ReasonCode,
    string PolicySnapshotId,
    OutboundChannelControlState OldState,
    OutboundChannelControlState NewState,
    long SourceVersion,
    string RequesterRef,
    string SchemaVersion,
    string CorrelationId) : IChatBotCommand;

/// <summary>
/// Second-person approval that activates a pending outbound-channel quarantine (FR75d). The approver MUST be a
/// different human from the proposer; this is re-checked in the aggregate as defense-in-depth. Mirrors
/// <see cref="ApproveOutboundChannelDisable"/>.
/// </summary>
public sealed record ApproveOutboundChannelQuarantine(
    string QuarantineChangeId,
    string OutboundChannelRef,
    string ReasonCode,
    string PolicySnapshotId,
    OutboundChannelControlState OldState,
    OutboundChannelControlState NewState,
    long SourceVersion,
    string RequesterRef,
    string ApproverRef,
    string SchemaVersion,
    string CorrelationId) : IChatBotCommand;

/// <summary>
/// Known schema versions for the FR74/FR75 outbound-channel rate-limit (Story 7.26) command. A dedicated constant —
/// not <see cref="OutboundChannelControlSchemaVersions.V1"/> — because the rate-limit command shape diverges from the
/// disable/quarantine two-person control commands: it is single-actor and carries a bounded budget + window token
/// instead of <c>OutboundChannelControlState</c> old/new-state fields. Mirrors
/// <see cref="CommandCapabilityRateLimitSchemaVersions"/>.
/// </summary>
public static class OutboundChannelRateLimitSchemaVersions
{
    public const string V1 = "outbound-channel-rate-limit-schema.v1";

    public static IReadOnlySet<string> All { get; } =
        new HashSet<string>([V1], StringComparer.Ordinal);

    public static bool IsKnown(string? schemaVersion)
        => !string.IsNullOrWhiteSpace(schemaVersion) && All.Contains(schemaVersion);
}

/// <summary>
/// Single-actor, schema-bounded proposal to rate-limit an outbound channel — the governed external-send path,
/// identified by its safe <see cref="OutboundChannelRef"/> (the <c>AdapterRef</c> token, e.g.
/// <c>adapter:mailbox-outbound</c>) — for a tenant (Story 7.26, FR74/FR75). Unlike the security-sensitive
/// outbound-channel disable/quarantine controls (Stories 7.24/7.25), rate-limit is a <em>standard policy mutation</em>
/// per the FR74 decomposition guidance: it follows the single-actor <see cref="SubmitCommandCapabilityRateLimit"/> /
/// <see cref="SubmitAiActorRateLimit"/> authorization shape (one authority check, no approver) — there is no
/// <c>Approve…</c> counterpart, no distinct-approver guard, and no <see cref="OutboundChannelControlState"/>
/// transition. "Old budget"/"new budget" are the prior and new per-window send budgets, not a control state.
/// Outbound-channel governance is a security-sensitive policy concern, so it gates on the policy-admin scope (the
/// same as the disable/quarantine pairs). Tenant authority is supplied by the authenticated gateway binding, never
/// the command body. Carries only safe, finite, metadata-only tokens — never recipient/sender addresses, message
/// content, credentials, OAuth grant fingerprints, model prompts/completions, or delegated-user PII. The budget is
/// bounded by <see cref="OutboundChannelRateLimitBounds"/>. Enforcement is the outbound send seam's final gate (after
/// the Disabled/Quarantined control-state switch), where the channel ref and the authenticated tenant binding meet
/// immediately before the external adapter call.
/// </summary>
public sealed record SubmitOutboundChannelRateLimit(
    string RateLimitChangeId,
    string OutboundChannelRef,
    string ReasonCode,
    string PolicySnapshotId,
    int OldBudget,
    int NewBudget,
    OutboundChannelRateLimitWindow Window,
    long SourceVersion,
    string RequesterRef,
    string SchemaVersion,
    string CorrelationId) : IChatBotCommand;
