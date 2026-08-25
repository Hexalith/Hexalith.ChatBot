namespace Hexalith.ChatBot.UI.Design;

/// <summary>
/// A single prioritized, grouped approval row for the reviewer work surface (Story 7.8). The group is keyed by the
/// safe <c>(requester × command × project)</c> fingerprint and carries the safe priority label/explanation token plus
/// the per-item count rendered on the one primary batch action. All values are safe refs/tokens or bounded integers —
/// never project names, evidence, recipient PII, audit reasons, or command bodies.
/// </summary>
/// <param name="GroupKey">The safe <c>sha256:</c> group fingerprint.</param>
/// <param name="RequesterRef">Safe requester ref label (redacted/omitted when unauthorized).</param>
/// <param name="CommandRef">Safe command token.</param>
/// <param name="ProjectRef">Safe project ref label (redacted/omitted when unauthorized).</param>
/// <param name="PriorityLabel">Plain-language priority headline preceding the raw token.</param>
/// <param name="PriorityExplanation">Safe single-token priority explanation (no spaces, ascii-safe).</param>
/// <param name="ItemCount">The number of underlying approval items in the group (≥ 1) shown on the batch action.</param>
public sealed record ChatBotApprovalPriorityGroupRow(
    string GroupKey,
    string RequesterRef,
    string CommandRef,
    string ProjectRef,
    string PriorityLabel,
    string PriorityExplanation,
    int ItemCount);

/// <summary>
/// Contract bundle for the prioritized/grouped approval-queue reviewer surface (Story 7.8). Mirrors
/// <see cref="ChatBotEscalationPolicyEditorContract"/>: a dense work surface rendering the prioritized order
/// (highest-first, with a visible safe priority label/explanation), grouped rows with a safe group header
/// (requester/command/project labels where authorized), one primary batch approve/reject action per group showing the
/// per-item count, a partial-authority disabled-action explanation, validation summary, recovery, focus-return, and a
/// phone fallback. No raw JSON, no hover-only critical actions, no new design system, no infinite scroll.
/// </summary>
/// <param name="Validation">Validation summary and field association contract.</param>
/// <param name="Recovery">Save/recovery and partial-outcome contract.</param>
/// <param name="SmallScreenFallback">Phone-limited fallback contract.</param>
/// <param name="DisabledBatchAction">Disabled batch-action contract (partial-authority explanation).</param>
/// <param name="FocusReturn">Focus-return contract for the batch confirmation/partial-outcome panel.</param>
/// <param name="Groups">The prioritized, grouped approval rows rendered highest-first.</param>
/// <param name="ShownPriorityMetadata">Safe snapshot/priority metadata rows shown by the surface.</param>
/// <param name="RestrictedMarkers">Markers that must not appear in safe UI copy.</param>
public sealed record ChatBotApprovalQueuePriorityContract(
    ChatBotValidationErrorContract Validation,
    ChatBotRecoveryPatternContract Recovery,
    ChatBotSmallScreenFallbackContract SmallScreenFallback,
    ChatBotDisabledActionContract DisabledBatchAction,
    ChatBotFocusReturnContract FocusReturn,
    IReadOnlyList<ChatBotApprovalPriorityGroupRow> Groups,
    IReadOnlyList<string> ShownPriorityMetadata,
    IReadOnlyList<string> RestrictedMarkers)
{
    /// <summary>Gets a value indicating whether the approval-queue priority contract is complete and metadata-only.</summary>
    public bool IsComplete
        => Validation.IsComplete
            && Recovery.IsComplete
            && SmallScreenFallback.IsComplete
            && DisabledBatchAction.IsComplete
            && FocusReturn.IsComplete
            && Groups is { Count: > 0 }
            && Groups.All(static row =>
                !string.IsNullOrWhiteSpace(row.GroupKey)
                && !string.IsNullOrWhiteSpace(row.RequesterRef)
                && !string.IsNullOrWhiteSpace(row.CommandRef)
                && !string.IsNullOrWhiteSpace(row.ProjectRef)
                && !string.IsNullOrWhiteSpace(row.PriorityLabel)
                && !string.IsNullOrWhiteSpace(row.PriorityExplanation))
            && GroupsAreBounded
            && ShownPriorityMetadata is { Count: > 0 }
            && ShownPriorityMetadata.All(static value => !string.IsNullOrWhiteSpace(value))
            && PrioritizedHighestFirst
            && !ContainsRestrictedText;

    /// <summary>Gets a value indicating whether every group row is bounded: a safe space-free fingerprint/explanation and a per-item count of at least one.</summary>
    public bool GroupsAreBounded => Groups.All(GroupBounded);

    /// <summary>
    /// Gets a value indicating whether the rows are ordered highest-priority-first (the dense triage requirement).
    /// A row whose priority label is outside the known vocabulary makes this false rather than ranking it 0: an
    /// unrecognised label previously collapsed every comparison to <c>0 &gt;= 0</c> and satisfied the invariant
    /// vacuously.
    /// </summary>
    public bool PrioritizedHighestFirst
        => Groups.All(static row => PriorityRank(row) is not null)
            && Groups
                .Zip(Groups.Skip(1), static (current, next) => (current, next))
                .All(static pair => PriorityRank(pair.current) >= PriorityRank(pair.next));

    /// <summary>Gets a value indicating whether restricted markers leak into visible metadata.</summary>
    /// <remarks>
    /// The scan covers every string the surface renders, not just the group rows: the disabled batch-action
    /// explanation, the phone-fallback copy and the validation next-action are all displayed and were previously
    /// outside the leak check.
    /// </remarks>
    public bool ContainsRestrictedText
        => RestrictedMarkers
                .Where(static marker => !string.IsNullOrWhiteSpace(marker))
                .Any(marker =>
                    ShownPriorityMetadata.Any(value => value.Contains(marker, StringComparison.OrdinalIgnoreCase))
                    || RenderedCopy.Any(value => value.Contains(marker, StringComparison.OrdinalIgnoreCase))
                    || Groups.Any(row =>
                        row.GroupKey.Contains(marker, StringComparison.OrdinalIgnoreCase)
                        || row.RequesterRef.Contains(marker, StringComparison.OrdinalIgnoreCase)
                        || row.CommandRef.Contains(marker, StringComparison.OrdinalIgnoreCase)
                        || row.ProjectRef.Contains(marker, StringComparison.OrdinalIgnoreCase)
                        || row.PriorityLabel.Contains(marker, StringComparison.OrdinalIgnoreCase)
                        || row.PriorityExplanation.Contains(marker, StringComparison.OrdinalIgnoreCase)));

    // Every non-group string the surface renders. Kept beside the leak check so a new rendered field is added here
    // rather than silently escaping it.
    private IEnumerable<string> RenderedCopy
        =>
        [
            DisabledBatchAction.ActionName,
            DisabledBatchAction.DisabledReasonLabel,
            SmallScreenFallback.ReadOnlySummary,
            SmallScreenFallback.CurrentStatus,
            SmallScreenFallback.HandoffLinkLabel,
            SmallScreenFallback.LargerScreenGuidance,
            SmallScreenFallback.ReachableExplanation,
            Validation.SummaryLabel,
            Validation.SafeNextAction,
        ];

    private static bool GroupBounded(ChatBotApprovalPriorityGroupRow row)
        => row.ItemCount >= 1
            && row.GroupKey.StartsWith("sha256:", StringComparison.Ordinal)
            && !row.GroupKey.Contains(' ', StringComparison.Ordinal)
            && !row.PriorityExplanation.Contains(' ', StringComparison.Ordinal);

    // The default contract carries pre-ordered rows; the rank is the row's position-independent priority token rendered
    // for sorting display. Higher sorts first.
    private static int? PriorityRank(ChatBotApprovalPriorityGroupRow row)
        => row.PriorityLabel switch
        {
            "Critical" => 4,
            "High" => 3,
            "Medium" => 2,
            "Low" => 1,
            _ => null,
        };

    /// <summary>Creates the default approval-queue priority contract used by design and bUnit tests.</summary>
    /// <returns>A complete, metadata-only approval-queue priority contract.</returns>
    public static ChatBotApprovalQueuePriorityContract CreateDefault()
    {
        var fields = new Dictionary<string, string>
        {
            ["approval-queue-batch-reason"] = "approval-queue-batch-reason-message",
        };

        string[] restrictedMarkers = ["project name", "mailbox body", "provider payload", "raw claim", "token", "secret", "prompt", "command body"];

        // Rows are pre-ordered highest-priority-first (the projector renders the deterministic order).
        ChatBotApprovalPriorityGroupRow[] groups =
        [
            new("sha256:11aa", "requester:requester-1", "command:appendconversationmessage", "project:project-1", "Critical", "risk:blocked|authority:send-on-behalf|age:7200s", 3),
            new("sha256:22bb", "requester:requester-2", "command:createoutbounddraft", "project:project-2", "High", "risk:high|authority:shared-mailbox-send|age:3600s", 2),
            new("sha256:33cc", "requester:requester-3", "command:appendconversationmessage", "project:project-3", "Medium", "risk:medium|authority:authenticated-user-send|age:600s", 1),
        ];

        return new(
            new ChatBotValidationErrorContract(
                "approval-queue-priority-validation-summary",
                "Approval queue priority validation summary",
                "approval-queue-priority-validation-summary",
                fields.Keys.ToArray(),
                fields,
                "Review the partial-outcome summary before submitting the next batch decision."),
            ChatBotRecoveryPatternContract.ForTenantConfiguration(
                "stale_data",
                "approval-queue-priority-validation-summary",
                "before-fields",
                fields,
                ChatBotSaveConflictCause.StaleData,
                "Review the partial-outcome summary before submitting the next batch decision.",
                restrictedMarkers),
            ChatBotSmallScreenFallbackContract.CreatePhoneLimited(
                "Prioritized approval summary is available on phone.",
                "active-pending-approval-queue",
                ["review-summary", "open-approval-group"],
                "Open prioritized approval queue",
                "Use a larger screen for dense batch approvals.",
                "approval-queue-batch-draft-preserved",
                "Dense batch controls are unavailable on this screen size; the prioritized summary and safe per-group action remain reachable."),
            ChatBotDisabledActionContract.CreateGovernedAction(
                "Approve approval group",
                "approval-queue-batch-disabled-reason",
                "Per-project approval authority is required for every item in the group before a batch decision; unauthorized items are decided individually with a safe reason."),
            ChatBotFocusReturnContract.ForOverlay(ChatBotOverlayKind.ReviewPanel),
            groups,
            ["group-fingerprint", "priority-score", "priority-explanation", "per-item-count", "safe-conflict-cause"],
            restrictedMarkers);
    }
}
