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
