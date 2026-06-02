using Hexalith.ChatBot.Contracts.Enums;

namespace Hexalith.ChatBot.Contracts.Commands;

/// <summary>
/// Known schema versions for the FR74 AI-actor control (disable, quarantine) commands.
/// </summary>
public static class AiActorControlSchemaVersions
{
    public const string V1 = "ai-actor-control-schema.v1";

    public static IReadOnlySet<string> All { get; } =
        new HashSet<string>([V1], StringComparer.Ordinal);

    public static bool IsKnown(string? schemaVersion)
        => !string.IsNullOrWhiteSpace(schemaVersion) && All.Contains(schemaVersion);
}

/// <summary>
/// Known schema versions for the FR74/FR75 AI-actor rate-limit (Story 7.20) command. A dedicated constant —
/// not <see cref="AiActorControlSchemaVersions.V1"/> — because the rate-limit command shape diverges from the
/// disable/quarantine two-person control commands: it is single-actor and carries a bounded budget + window token
/// instead of <c>AiActorControlState</c> old/new-state fields. Mirrors
/// <see cref="ServiceClientRateLimitSchemaVersions"/>.
/// </summary>
public static class AiActorRateLimitSchemaVersions
{
    public const string V1 = "ai-actor-rate-limit-schema.v1";

    public static IReadOnlySet<string> All { get; } =
        new HashSet<string>([V1], StringComparer.Ordinal);

    public static bool IsKnown(string? schemaVersion)
        => !string.IsNullOrWhiteSpace(schemaVersion) && All.Contains(schemaVersion);
}

/// <summary>
/// First-person proposal to disable an AI actor under the FR75d two-person rule. Tenant and actor authority are
/// supplied by the authenticated gateway binding, never the command body. Carries only safe, finite,
/// metadata-only tokens — never service-client/AI credentials, OAuth grant fingerprints, model
/// prompts/completions, delegated-user PII, or addresses. The subject is identified by its safe
/// <c>ServiceClientId</c> (the AI actor's <see cref="AiActorRef"/>).
/// </summary>
public sealed record SubmitAiActorDisable(
    string DisableChangeId,
    string AiActorRef,
    string ReasonCode,
    string PolicySnapshotId,
    AiActorControlState OldState,
    AiActorControlState NewState,
    long SourceVersion,
    string RequesterRef,
    string SchemaVersion,
    string CorrelationId) : IChatBotCommand;

/// <summary>
/// Second-person approval that activates a pending AI-actor disable (FR75d). The approver MUST be a different
/// human from the proposer; this is re-checked in the aggregate as defense-in-depth.
/// </summary>
public sealed record ApproveAiActorDisable(
    string DisableChangeId,
    string AiActorRef,
    string ReasonCode,
    string PolicySnapshotId,
    AiActorControlState OldState,
    AiActorControlState NewState,
    long SourceVersion,
    string RequesterRef,
    string ApproverRef,
    string SchemaVersion,
    string CorrelationId) : IChatBotCommand;

/// <summary>
/// First-person proposal to quarantine an AI actor under the FR75d two-person rule — its new proposals and
/// commands enter a review-only/contained state and fail closed at the command-admission pipeline until the
/// quarantine is cleared. Tenant and actor authority are supplied by the authenticated gateway binding, never
/// the command body. Carries only safe, finite, metadata-only tokens — never service-client/AI credentials,
/// OAuth grant fingerprints, model prompts/completions, delegated-user PII, or addresses. The subject is
/// identified by its safe <c>ServiceClientId</c> (the AI actor's <see cref="AiActorRef"/>). Reuses
/// <see cref="AiActorControlSchemaVersions.V1"/> — the command shape is identical to the disable pair.
/// </summary>
public sealed record SubmitAiActorQuarantine(
    string QuarantineChangeId,
    string AiActorRef,
    string ReasonCode,
    string PolicySnapshotId,
    AiActorControlState OldState,
    AiActorControlState NewState,
    long SourceVersion,
    string RequesterRef,
    string SchemaVersion,
    string CorrelationId) : IChatBotCommand;

/// <summary>
/// Second-person approval that activates a pending AI-actor quarantine (FR75d). The approver MUST be a different
/// human from the proposer; this is re-checked in the aggregate as defense-in-depth.
/// </summary>
public sealed record ApproveAiActorQuarantine(
    string QuarantineChangeId,
    string AiActorRef,
    string ReasonCode,
    string PolicySnapshotId,
    AiActorControlState OldState,
    AiActorControlState NewState,
    long SourceVersion,
    string RequesterRef,
    string ApproverRef,
    string SchemaVersion,
    string CorrelationId) : IChatBotCommand;

/// <summary>
/// Single-actor, schema-bounded proposal to rate-limit a noisy AI actor (Story 7.20, FR74/FR75). Unlike the
/// security-sensitive AI-actor disable/quarantine controls, rate-limit is a <em>standard policy mutation</em> per the
/// FR74 decomposition guidance: it follows the single-actor <see cref="SubmitServiceClientRateLimit"/> authorization
/// shape (one authority check, no approver) — there is no <c>Approve…</c> counterpart, no distinct-approver guard, and
/// no <see cref="AiActorControlState"/> transition. "Old budget"/"new budget" are the prior and new per-window
/// proposal budgets, not a control state. Authorization is the AI-action policy-admin's domain (Story 7.2), so it gates
/// on the policy admin scope — the divergence from the tenant-admin-gated <see cref="SubmitServiceClientRateLimit"/>.
/// Tenant and actor authority are supplied by the authenticated gateway binding, never the command body. Carries only
/// safe, finite, metadata-only tokens — never service-client/AI credentials, OAuth grant fingerprints, model
/// prompts/completions, delegated-user PII, or addresses. The subject is identified by its safe <c>ServiceClientId</c>
/// (the AI actor's <see cref="AiActorRef"/>). The budget is bounded by <c>AiActorRateLimitBounds</c>.
/// </summary>
public sealed record SubmitAiActorRateLimit(
    string RateLimitChangeId,
    string AiActorRef,
    string ReasonCode,
    string PolicySnapshotId,
    int OldBudget,
    int NewBudget,
    AiActorRateLimitWindow Window,
    long SourceVersion,
    string RequesterRef,
    string SchemaVersion,
    string CorrelationId) : IChatBotCommand;
