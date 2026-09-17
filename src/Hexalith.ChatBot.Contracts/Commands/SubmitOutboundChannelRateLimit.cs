using Hexalith.ChatBot.Contracts.Enums;

namespace Hexalith.ChatBot.Contracts.Commands;

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
