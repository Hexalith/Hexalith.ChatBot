namespace Hexalith.ChatBot.UI.State.AssociationReview;

public sealed record LoadAssociationReviewAction(string AssociationId);

public sealed record AssociationReviewLoadedAction(AssociationReviewModel Review);

public sealed record AssociationReviewFailedAction(string ErrorCode);

public sealed record SelectAssociationCandidateAction(string CandidateId);

public sealed record UpdateAssociationDecisionNoteAction(string DecisionNote);

public sealed record UpdateAssociationCorrectionRationaleAction(string CorrectionRationale);

/// <summary>
/// Asks the surface to confirm a decision before anything durable happens. This action alone never submits a
/// command; it only opens the confirmation for <paramref name="DecisionCode"/>. The durable submit runs only
/// on <see cref="ConfirmAssociationDecisionAction"/>.
/// </summary>
public sealed record RequestAssociationDecisionAction(string DecisionCode);

/// <summary>Abandons a pending decision without submitting anything.</summary>
public sealed record CancelAssociationDecisionAction;

/// <summary>Submits the pending decision. This is the only action that writes a durable decision command.</summary>
public sealed record ConfirmAssociationDecisionAction;

public sealed record AssociationDecisionValidationRejectedAction(string ValidationErrorCode);

public sealed record AssociationDecisionSubmittedAction(AssociationDecisionSubmitResult Result);

public sealed record AssociationDecisionSubmitFailedAction(string ErrorCode);

public sealed record SubmitAssociationCorrectionAction;

public sealed record AssociationCorrectionValidationRejectedAction(string ValidationErrorCode);

public sealed record AssociationCorrectionSubmittedAction(AssociationCorrectionSubmitResult Result);

public sealed record AssociationCorrectionSubmitFailedAction(string ErrorCode);
