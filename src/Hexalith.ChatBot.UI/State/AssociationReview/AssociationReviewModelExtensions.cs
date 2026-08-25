namespace Hexalith.ChatBot.UI.State.AssociationReview;

/// <summary>
/// Single source of truth for lifecycle gates that both the surface and the effects must agree on. Two
/// independent copies previously diverged, rendering an enabled correction submit in Correcting and
/// Correction-delayed whose only possible outcome was a correction-invalid-lifecycle rejection.
/// </summary>
public static class AssociationReviewModelExtensions
{
    /// <summary>
    /// Gets a value indicating whether the lifecycle admits a correction. Correcting and Correction-delayed
    /// are excluded deliberately: a correction is already propagating, so the panel stays hidden until it
    /// settles rather than offering a second correction against an unsettled association.
    /// </summary>
    public static bool CanCorrect(string? lifecycleState)
        => lifecycleState is "Associated" or "Corrected";
}
