using Fluxor;

namespace Hexalith.ChatBot.UI.State.AssociationReview;

public sealed class AssociationReviewFeature : Feature<AssociationReviewState>
{
    public override string GetName() => "AssociationReview";

    protected override AssociationReviewState GetInitialState()
        => new(
            IsLoading: false,
            IsSubmitting: false,
            Review: null,
            SelectedCandidateId: null,
            DecisionNote: string.Empty,
            ErrorCode: null,
            ValidationErrorCode: null);
}
