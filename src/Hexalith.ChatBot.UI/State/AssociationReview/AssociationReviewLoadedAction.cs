namespace Hexalith.ChatBot.UI.State.AssociationReview;

public sealed record AssociationReviewLoadedAction(AssociationReviewModel Review)
{
    /// <summary>Gets the load whose response supplied the review.</summary>
    internal LoadAssociationReviewAction? OriginatingLoad { get; init; }
}
