using Hexalith.ChatBot.Contracts.Commands;
using Hexalith.ChatBot.Contracts.Enums;

namespace Hexalith.ChatBot.Server.Gateway.Stages;

/// <summary>
/// Pure planner that fans a grouped batch approve/reject out into <b>one governed single-item decision command per
/// underlying approval item</b> (Story 7.8, NFR46/FR75c/FR75g). It does NOT introduce a batch command or a collapsed
/// audit envelope: each produced <see cref="DecideAiActionApproval"/>/<see cref="DecideOutboundApproval"/> still flows
/// through the existing <c>auth → tenant-bind → authorize → risk-classify → approval-gate → idempotency →
/// pre-commit-audit → execute → post-commit-audit</c> spine, so the gateway emits exactly one audit event per item.
///
/// <para>Batching never elevates authority:</para>
/// <list type="bullet">
///   <item>Non-human actors (service/AI/automation without delegated human authority) are denied the whole batch
///   <b>before state load</b>; no item is acted on.</item>
///   <item>Per item, only items the reviewer is authorized for produce a command; items lacking authority record a safe
///   denial (<see cref="InsufficientAuthorityReasonCode"/>) with no existence leakage and do not block authorized
///   items.</item>
/// </list>
/// </summary>
internal static class ApprovalBatchDecisionPlanner
{
    public const string HumanActorValue = "human";
    public const string NonHumanActorReasonCode = "batch_actor_not_human";
    public const string InsufficientAuthorityReasonCode = "insufficient_authority";
    public const string AuthorizedReasonCode = "batch_authorized";
    public const string ItemAcceptedReasonCode = "approval-decision-authorized";

    public static BatchDecisionPlan Plan(
        string actorType,
        ApprovalDecisionKind decision,
        string groupKeyFingerprint,
        IReadOnlyList<BatchDecisionItem> items)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(groupKeyFingerprint);
        ArgumentNullException.ThrowIfNull(items);

        // Non-human actors are denied batch approval before any per-item state load (FR75c, NFR2).
        if (!string.Equals(actorType, HumanActorValue, StringComparison.Ordinal))
        {
            return new BatchDecisionPlan(false, NonHumanActorReasonCode, groupKeyFingerprint, []);
        }

        List<BatchDecisionOutcome> outcomes = new(items.Count);
        foreach (BatchDecisionItem item in items)
        {
            if (!item.ReviewerHasAuthority)
            {
                // Per-item gate denial — safe reason code, no command, no existence leakage. Other items proceed.
                outcomes.Add(new BatchDecisionOutcome(item.ApprovalId, false, null, InsufficientAuthorityReasonCode));
                continue;
            }

            IChatBotCommand command = BuildCommand(item, decision);
            outcomes.Add(new BatchDecisionOutcome(item.ApprovalId, true, command, ItemAcceptedReasonCode));
        }

        return new BatchDecisionPlan(true, AuthorizedReasonCode, groupKeyFingerprint, outcomes);
    }

    private static IChatBotCommand BuildCommand(BatchDecisionItem item, ApprovalDecisionKind decision)
        => item.Kind switch
        {
            BatchDecisionItemKind.AiAction => new DecideAiActionApproval(
                item.ProjectId,
                item.ApprovalId,
                item.ProposalId ?? string.Empty,
                item.SourceMessageId ?? string.Empty,
                decision,
                item.ExpectedApprovalSourceVersion,
                item.CorrelationId,
                item.DecisionId),
            BatchDecisionItemKind.Outbound => new DecideOutboundApproval(
                item.ApprovalId,
                item.DraftId ?? string.Empty,
                item.ProjectId,
                decision,
                item.DecisionId,
                item.ExpectedApprovalSourceVersion,
                item.CorrelationId),
            _ => throw new ArgumentOutOfRangeException(nameof(item), item.Kind, "Unsupported batch decision item kind."),
        };
}
