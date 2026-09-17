using Hexalith.ChatBot.Contracts.Enums;

namespace Hexalith.ChatBot.Contracts.Commands;

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
