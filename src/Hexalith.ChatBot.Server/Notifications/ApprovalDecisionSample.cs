using Hexalith.ChatBot.Contracts.Enums;

namespace Hexalith.ChatBot.Server.Notifications;

/// <summary>
/// A metadata-only approval-decision sample (Story 7.11, NFR46/NFR2) — the deterministic input to the
/// <see cref="ApprovalRubberStampRateEvaluator"/>. Carries only the safe timestamp/kind/risk/reviewer fields needed to
/// measure the rubber-stamp rate — never project/proposal/command content, evidence, or recipient PII.
/// <para>
/// The <see cref="TenantRef"/> comes from the authenticated gateway binding carried on the decision snapshot, never from
/// proposal/project/command refs or reviewer-supplied data. The <see cref="ReviewerRef"/> is the safe
/// <c>DecisionActorId</c> token (the reviewer who decided); a null/blank reviewer is excluded from per-reviewer
/// attribution but still counts toward the tenant aggregate. The runtime/Dapr projection over <c>ApprovalEventView</c>
/// that materializes these samples is the <strong>deferred</strong> caller concern; this story provides the deterministic
/// evaluator over a caller-supplied snapshot.
/// </para>
/// </summary>
/// <param name="TenantRef">The tenant ref from the authenticated binding (the only valid aggregation tenant key).</param>
/// <param name="ReviewerRef">The safe reviewer token (<c>DecisionActorId</c>), or <see langword="null"/> when absent.</param>
/// <param name="RequestedAtUtc">The server-stamped UTC time the approval was requested.</param>
/// <param name="DecidedAtUtc">The server-stamped UTC time the decision was recorded (the rolling-window key).</param>
/// <param name="DecisionKind">The decision kind; only <see cref="ApprovalDecisionKind.Approve"/> counts.</param>
/// <param name="AiRiskClass">The originating action's risk class; only <see cref="AiActionRiskClass.ApprovalRequired"/> counts.</param>
internal sealed record ApprovalDecisionSample(
    string TenantRef,
    string? ReviewerRef,
    DateTimeOffset RequestedAtUtc,
    DateTimeOffset DecidedAtUtc,
    ApprovalDecisionKind DecisionKind,
    AiActionRiskClass AiRiskClass);
