using Hexalith.ChatBot.Contracts.Enums;

namespace Hexalith.ChatBot.Contracts.Commands;

/// <summary>
/// Known schema versions for the FR74 command-capability control (disable) commands. A dedicated constant —
/// not <see cref="AiActorControlSchemaVersions.V1"/> — because the subject diverges from the AI-actor control
/// plane: the command capability is identified by its safe command <em>type name</em>
/// (<c>CommandCapabilityRef</c>), not an actor id. Mirrors <see cref="AiActorControlSchemaVersions"/>.
/// </summary>
public static class CommandCapabilityControlSchemaVersions
{
    public const string V1 = "command-capability-control-schema.v1";

    public static IReadOnlySet<string> All { get; } =
        new HashSet<string>([V1], StringComparer.Ordinal);

    public static bool IsKnown(string? schemaVersion)
        => !string.IsNullOrWhiteSpace(schemaVersion) && All.Contains(schemaVersion);
}

/// <summary>
/// Known schema versions for the FR74/FR75 command-capability rate-limit (Story 7.23) command. A dedicated
/// constant — not <see cref="CommandCapabilityControlSchemaVersions.V1"/> — because the rate-limit command shape
/// diverges from the disable/quarantine two-person control commands: it is single-actor and carries a bounded
/// budget + window token instead of <c>CommandCapabilityControlState</c> old/new-state fields. Mirrors
/// <see cref="AiActorRateLimitSchemaVersions"/>.
/// </summary>
public static class CommandCapabilityRateLimitSchemaVersions
{
    public const string V1 = "command-capability-rate-limit-schema.v1";

    public static IReadOnlySet<string> All { get; } =
        new HashSet<string>([V1], StringComparer.Ordinal);

    public static bool IsKnown(string? schemaVersion)
        => !string.IsNullOrWhiteSpace(schemaVersion) && All.Contains(schemaVersion);
}

/// <summary>
/// First-person proposal to disable a command capability under the FR75d two-person rule — its future
/// submissions are removed from admitted execution for the tenant and fail closed at the command-admission
/// pipeline until the capability is re-enabled. Tenant authority is supplied by the authenticated gateway
/// binding, never the command body. Carries only safe, finite, metadata-only tokens — never credentials, OAuth
/// grant fingerprints, model prompts/completions, delegated-user PII, or addresses. The subject is identified by
/// its safe command type name (the <see cref="CommandCapabilityRef"/>), a finite stable identifier.
/// </summary>
public sealed record SubmitCommandCapabilityDisable(
    string DisableChangeId,
    string CommandCapabilityRef,
    string ReasonCode,
    string PolicySnapshotId,
    CommandCapabilityControlState OldState,
    CommandCapabilityControlState NewState,
    long SourceVersion,
    string RequesterRef,
    string SchemaVersion,
    string CorrelationId) : IChatBotCommand;

/// <summary>
/// Second-person approval that activates a pending command-capability disable (FR75d). The approver MUST be a
/// different human from the proposer; this is re-checked in the aggregate as defense-in-depth.
/// </summary>
public sealed record ApproveCommandCapabilityDisable(
    string DisableChangeId,
    string CommandCapabilityRef,
    string ReasonCode,
    string PolicySnapshotId,
    CommandCapabilityControlState OldState,
    CommandCapabilityControlState NewState,
    long SourceVersion,
    string RequesterRef,
    string ApproverRef,
    string SchemaVersion,
    string CorrelationId) : IChatBotCommand;

/// <summary>
/// First-person proposal to quarantine a command capability under the FR75d two-person rule — its future
/// submissions are paused (contained for review) for the tenant and fail closed at the command-admission pipeline
/// until the quarantine is cleared. Mirrors <see cref="SubmitCommandCapabilityDisable"/> (Story 7.21) with the
/// quarantine control-state substituted for disable (the Story 7.19 disable→quarantine precedent), reusing
/// <see cref="CommandCapabilityControlSchemaVersions.V1"/> because the command shape is identical. Tenant
/// authority is supplied by the authenticated gateway binding, never the command body. Carries only safe, finite,
/// metadata-only tokens — never credentials, OAuth grant fingerprints, model prompts/completions, delegated-user
/// PII, or addresses. The subject is identified by its safe command type name (the
/// <see cref="CommandCapabilityRef"/>), a finite stable identifier.
/// </summary>
public sealed record SubmitCommandCapabilityQuarantine(
    string QuarantineChangeId,
    string CommandCapabilityRef,
    string ReasonCode,
    string PolicySnapshotId,
    CommandCapabilityControlState OldState,
    CommandCapabilityControlState NewState,
    long SourceVersion,
    string RequesterRef,
    string SchemaVersion,
    string CorrelationId) : IChatBotCommand;

/// <summary>
/// Second-person approval that activates a pending command-capability quarantine (FR75d). The approver MUST be a
/// different human from the proposer; this is re-checked in the aggregate as defense-in-depth.
/// </summary>
public sealed record ApproveCommandCapabilityQuarantine(
    string QuarantineChangeId,
    string CommandCapabilityRef,
    string ReasonCode,
    string PolicySnapshotId,
    CommandCapabilityControlState OldState,
    CommandCapabilityControlState NewState,
    long SourceVersion,
    string RequesterRef,
    string ApproverRef,
    string SchemaVersion,
    string CorrelationId) : IChatBotCommand;

/// <summary>
/// Single-actor, schema-bounded proposal to rate-limit a command capability — a governed command <em>type</em> —
/// for a tenant (Story 7.23, FR74/FR75). Unlike the security-sensitive command-capability disable/quarantine
/// controls (Stories 7.21/7.22), rate-limit is a <em>standard policy mutation</em> per the FR74 decomposition
/// guidance: it follows the single-actor <see cref="SubmitAiActorRateLimit"/> authorization shape (one authority
/// check, no approver) — there is no <c>Approve…</c> counterpart, no distinct-approver guard, and no
/// <see cref="CommandCapabilityControlState"/> transition. "Old budget"/"new budget" are the prior and new
/// per-window command budgets, not a control state. Command-capability governance is a security-sensitive policy
/// concern, so it gates on the policy-admin scope (the same as the disable/quarantine pairs). Tenant authority is
/// supplied by the authenticated gateway binding, never the command body. Carries only safe, finite, metadata-only
/// tokens — never credentials, OAuth grant fingerprints, model prompts/completions, delegated-user PII, or
/// addresses. The subject is identified by its safe command type name (the <see cref="CommandCapabilityRef"/>), a
/// finite stable identifier. The budget is bounded by <c>CommandCapabilityRateLimitBounds</c>. Because the subject
/// is a command type submitted by ANY actor (human/service/AI), enforcement is the actor-agnostic admission seam's
/// final gate, not the per-actor grant validator.
/// </summary>
public sealed record SubmitCommandCapabilityRateLimit(
    string RateLimitChangeId,
    string CommandCapabilityRef,
    string ReasonCode,
    string PolicySnapshotId,
    int OldBudget,
    int NewBudget,
    CommandCapabilityRateLimitWindow Window,
    long SourceVersion,
    string RequesterRef,
    string SchemaVersion,
    string CorrelationId) : IChatBotCommand;
