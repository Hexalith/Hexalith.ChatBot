using Hexalith.ChatBot.Contracts.Commands;
using Hexalith.ChatBot.Contracts.Enums;

namespace Hexalith.ChatBot.Server.Gateway.Stages;

/// <summary>The underlying decision-command family a grouped approval item fans out to.</summary>
internal enum BatchDecisionItemKind
{
    AiAction,
    Outbound,
}

/// <summary>
/// A single underlying approval item in a batch approve/reject request, paired with the upstream per-item authority
/// resolution (<see cref="ReviewerHasAuthority"/>) drawn from the same scope/gate signal the per-item
/// <see cref="AiActionApprovalGate"/> enforces. All fields are safe refs/metadata — never approval content.
/// </summary>
internal sealed record BatchDecisionItem(
    BatchDecisionItemKind Kind,
    string ApprovalId,
    string ProjectId,
    long ExpectedApprovalSourceVersion,
    bool ReviewerHasAuthority,
    string CorrelationId,
    string DecisionId,
    string? ProposalId = null,
    string? SourceMessageId = null,
    string? DraftId = null);

/// <summary>The per-item result of fanning out a batch decision: either a dispatchable command or a safe denial.</summary>
/// <param name="ApprovalId">The underlying approval id (safe ref).</param>
/// <param name="Accepted">Whether the item will be dispatched as its own governed decision command.</param>
/// <param name="Command">The single-item decision command to dispatch, or <see langword="null"/> when denied.</param>
/// <param name="ReasonCode">The safe per-item reason code (no existence leakage).</param>
internal sealed record BatchDecisionOutcome(
    string ApprovalId,
    bool Accepted,
    IChatBotCommand? Command,
    string ReasonCode);

/// <summary>The plan produced for a batch approve/reject request.</summary>
/// <param name="Authorized">
/// <see langword="false"/> when the actor is denied batch approval before state load (non-human actor); then
/// <see cref="Outcomes"/> is empty and no item is acted on.
/// </param>
/// <param name="ReasonCode">The batch-level reason code.</param>
/// <param name="GroupKeyFingerprint">The safe <c>sha256:</c> group fingerprint carried into each per-item audit envelope.</param>
/// <param name="Outcomes">The per-item outcomes (empty when the batch is denied before state load).</param>
internal sealed record BatchDecisionPlan(
    bool Authorized,
    string ReasonCode,
    string GroupKeyFingerprint,
    IReadOnlyList<BatchDecisionOutcome> Outcomes)
{
    public int AcceptedCount => Outcomes.Count(static outcome => outcome.Accepted);

    public int DeniedCount => Outcomes.Count(static outcome => !outcome.Accepted);

    public IEnumerable<IChatBotCommand> Commands => Outcomes
        .Where(static outcome => outcome is { Accepted: true, Command: not null })
        .Select(static outcome => outcome.Command!);
}

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
