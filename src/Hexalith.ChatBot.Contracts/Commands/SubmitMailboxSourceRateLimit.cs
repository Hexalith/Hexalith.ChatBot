using Hexalith.ChatBot.Contracts.Enums;

namespace Hexalith.ChatBot.Contracts.Commands;

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
