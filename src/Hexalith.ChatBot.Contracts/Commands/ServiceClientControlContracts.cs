using Hexalith.ChatBot.Contracts.Enums;

namespace Hexalith.ChatBot.Contracts.Commands;

/// <summary>
/// Known schema versions for the FR74 service-client control (disable/quarantine) commands.
/// </summary>
public static class ServiceClientControlSchemaVersions
{
    public const string V1 = "service-client-control-schema.v1";

    public static IReadOnlySet<string> All { get; } =
        new HashSet<string>([V1], StringComparer.Ordinal);

    public static bool IsKnown(string? schemaVersion)
        => !string.IsNullOrWhiteSpace(schemaVersion) && All.Contains(schemaVersion);
}

/// <summary>
/// Known schema versions for the FR74/FR75 service-client rate-limit (Story 7.17) command. A dedicated constant —
/// not <see cref="ServiceClientControlSchemaVersions.V1"/> — because the rate-limit command shape diverges from the
/// disable/quarantine two-person control commands: it is single-actor and carries a bounded budget + window token
/// instead of <c>ServiceClientControlState</c> old/new-state fields.
/// </summary>
public static class ServiceClientRateLimitSchemaVersions
{
    public const string V1 = "service-client-rate-limit-schema.v1";

    public static IReadOnlySet<string> All { get; } =
        new HashSet<string>([V1], StringComparer.Ordinal);

    public static bool IsKnown(string? schemaVersion)
        => !string.IsNullOrWhiteSpace(schemaVersion) && All.Contains(schemaVersion);
}

/// <summary>
/// First-person proposal to disable a service client under the FR75d two-person rule. Tenant and actor
/// authority are supplied by the authenticated gateway binding, never the command body. Carries only safe,
/// finite, metadata-only tokens — never service-client credentials, OAuth grant fingerprints, delegated-user
/// PII, or addresses. The subject is identified by its safe <c>ServiceClientId</c>.
/// </summary>
public sealed record SubmitServiceClientDisable(
    string DisableChangeId,
    string ServiceClientRef,
    string ReasonCode,
    string PolicySnapshotId,
    ServiceClientControlState OldState,
    ServiceClientControlState NewState,
    long SourceVersion,
    string RequesterRef,
    string SchemaVersion,
    string CorrelationId) : IChatBotCommand;

/// <summary>
/// Second-person approval that activates a pending service-client disable (FR75d). The approver MUST be a
/// different human from the proposer; this is re-checked in the aggregate as defense-in-depth.
/// </summary>
public sealed record ApproveServiceClientDisable(
    string DisableChangeId,
    string ServiceClientRef,
    string ReasonCode,
    string PolicySnapshotId,
    ServiceClientControlState OldState,
    ServiceClientControlState NewState,
    long SourceVersion,
    string RequesterRef,
    string ApproverRef,
    string SchemaVersion,
    string CorrelationId) : IChatBotCommand;

/// <summary>
/// First-person proposal to quarantine (contain for review) a service client under the FR75d two-person rule.
/// Tenant and actor authority are supplied by the authenticated gateway binding, never the command body. Carries
/// only safe, finite, metadata-only tokens — never service-client credentials, OAuth grant fingerprints,
/// delegated-user PII, or addresses. The subject is identified by its safe <c>ServiceClientId</c>. The command
/// shape mirrors <see cref="SubmitServiceClientDisable"/> exactly and reuses
/// <see cref="ServiceClientControlSchemaVersions.V1"/>; the FR74 control state differs (Active→Quarantined).
/// </summary>
public sealed record SubmitServiceClientQuarantine(
    string QuarantineChangeId,
    string ServiceClientRef,
    string ReasonCode,
    string PolicySnapshotId,
    ServiceClientControlState OldState,
    ServiceClientControlState NewState,
    long SourceVersion,
    string RequesterRef,
    string SchemaVersion,
    string CorrelationId) : IChatBotCommand;

/// <summary>
/// Second-person approval that activates a pending service-client quarantine (FR75d). The approver MUST be a
/// different human from the proposer; this is re-checked in the aggregate as defense-in-depth.
/// </summary>
public sealed record ApproveServiceClientQuarantine(
    string QuarantineChangeId,
    string ServiceClientRef,
    string ReasonCode,
    string PolicySnapshotId,
    ServiceClientControlState OldState,
    ServiceClientControlState NewState,
    long SourceVersion,
    string RequesterRef,
    string ApproverRef,
    string SchemaVersion,
    string CorrelationId) : IChatBotCommand;

/// <summary>
/// Single-actor, schema-bounded proposal to rate-limit a noisy service client (Story 7.17, FR74/FR75). Unlike the
/// security-sensitive disable/quarantine controls, rate-limit is a <em>standard policy mutation</em> per the FR74
/// decomposition guidance: it follows the single-actor <see cref="SubmitMailboxSourceRateLimit"/> authorization
/// shape (one <c>HasHumanTenantAdmin</c> check) — there is no <c>Approve…</c> counterpart, no distinct-approver
/// guard, and no <see cref="ServiceClientControlState"/> transition. "Old budget"/"new budget" are the prior and new
/// per-window command budgets, not a control state. Tenant and actor authority are supplied by the authenticated
/// gateway binding, never the command body. Carries only safe, finite, metadata-only tokens — never service-client
/// credentials, OAuth grant fingerprints, delegated-user PII, or addresses. The subject is identified by its safe
/// <c>ServiceClientId</c>. The budget is bounded by <see cref="ServiceClientRateLimitBounds"/>.
/// </summary>
public sealed record SubmitServiceClientRateLimit(
    string RateLimitChangeId,
    string ServiceClientRef,
    string ReasonCode,
    string PolicySnapshotId,
    int OldBudget,
    int NewBudget,
    ServiceClientRateLimitWindow Window,
    long SourceVersion,
    string RequesterRef,
    string SchemaVersion,
    string CorrelationId) : IChatBotCommand;
