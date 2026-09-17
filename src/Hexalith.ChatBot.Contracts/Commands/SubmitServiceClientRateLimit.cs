using Hexalith.ChatBot.Contracts.Enums;

namespace Hexalith.ChatBot.Contracts.Commands;

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
