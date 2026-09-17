namespace Hexalith.ChatBot.UI.State.AssociationReview;

/// <summary>
/// Which operation produced the current <see cref="AssociationReviewState.ErrorCode"/>. A failed read and a
/// failed write are opposite messages: telling a reviewer their submission did not complete when nothing was
/// ever submitted invites a duplicate command.
/// </summary>
public enum AssociationReviewErrorScope
{
    None = 0,
    Load = 1,
    Submit = 2,
}
