namespace Hexalith.ChatBot.UI.State.AssociationReview;

public sealed record AssociationCorrectionSubmitFailedAction(string ErrorCode)
{
    /// <summary>Gets the review load that was active when the submission started.</summary>
    internal LoadAssociationReviewAction? OriginatingLoad { get; init; }
}
