namespace Hexalith.ChatBot.UI.State.AssociationReview;

public sealed record LoadAssociationReviewAction(string AssociationId);

public sealed record AssociationReviewLoadedAction(AssociationReviewModel Review);

public sealed record AssociationReviewFailedAction(string ErrorCode);

public sealed record SelectAssociationCandidateAction(string CandidateId);

public sealed record UpdateAssociationDecisionNoteAction(string DecisionNote);

public sealed record UpdateAssociationCorrectionRationaleAction(string CorrectionRationale);

public sealed record PreviewAssociationDecisionAction(string DecisionCode);

public sealed record AssociationDecisionPreviewRejectedAction(string ValidationErrorCode);

public sealed record AssociationDecisionSubmittedAction(AssociationDecisionSubmitResult Result);

public sealed record AssociationDecisionSubmitFailedAction(string ErrorCode);

public sealed record SubmitAssociationCorrectionAction;

public sealed record AssociationCorrectionValidationRejectedAction(string ValidationErrorCode);

public sealed record AssociationCorrectionSubmittedAction(AssociationCorrectionSubmitResult Result);

public sealed record AssociationCorrectionSubmitFailedAction(string ErrorCode);
