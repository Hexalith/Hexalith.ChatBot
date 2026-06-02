namespace Hexalith.ChatBot.Server.Notifications;

/// <summary>
/// The per-<c>(tenant × reviewer)</c> rubber-stamp breakdown carried for diagnosis (Story 7.11, AC1). Metadata-only —
/// the safe reviewer ref plus the rubber-stamp / qualifying-approval counts. The fraction itself is derived from the
/// counts (exact rational), never a stored float.
/// </summary>
/// <param name="ReviewerRef">The safe reviewer token the breakdown is attributed to.</param>
/// <param name="RubberStampCount">The reviewer's count of approvals decided in &lt; 5 s in the window.</param>
/// <param name="ApprovalTotal">The reviewer's count of qualifying approvals (denominator) in the window.</param>
internal sealed record ReviewerRubberStampRate(string ReviewerRef, int RubberStampCount, int ApprovalTotal);

/// <summary>
/// The metadata-only outcome of a rubber-stamp-rate evaluation pass (Story 7.11, NFR46/NFR2/FR41). Carries the
/// tenant-level rubber-stamp / qualifying-approval counts, the derived permille rate (an integer floor, exact;
/// the exact rational is the count + total pair), whether the FR41 tuning-revisit condition fired at the tenant level,
/// and the per-reviewer breakdown for diagnosis. Content-free — counts, the derived rate, safe reviewer refs, and the
/// three fixed governance constants only; never project/proposal/command content, evidence, or recipient PII.
/// </summary>
/// <param name="TenantRef">The tenant ref from the authenticated binding the rate is keyed by.</param>
/// <param name="CorrelationId">The evaluation-pass correlation id.</param>
/// <param name="RubberStampCount">The tenant-aggregate count of approvals decided in &lt; 5 s in the window.</param>
/// <param name="ApprovalTotal">The tenant-aggregate count of qualifying approvals (the denominator) in the window.</param>
/// <param name="RubberStampRatePermille">The integer-floor permille rate (0 when the denominator is zero); the exact
/// rational is <see cref="RubberStampCount"/> ÷ <see cref="ApprovalTotal"/>.</param>
/// <param name="TuningRevisitTriggered">Whether the tenant-level fraction strictly exceeded the NFR46 fatigue fraction.</param>
/// <param name="PerReviewer">The deterministic per-<c>(tenant × reviewer)</c> breakdown (null/blank reviewers excluded).</param>
internal sealed record ApprovalRubberStampRateObservation(
    string TenantRef,
    string CorrelationId,
    int RubberStampCount,
    int ApprovalTotal,
    int RubberStampRatePermille,
    bool TuningRevisitTriggered,
    IReadOnlyList<ReviewerRubberStampRate> PerReviewer);
