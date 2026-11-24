namespace Hexalith.ChatBot.UI.State.AssociationReview;

public sealed record AssociationReviewFailedAction(string ErrorCode)
{
    /// <summary>Gets the load that originated a route-specific failure.</summary>
    internal LoadAssociationReviewAction? OriginatingLoad { get; init; }
}
