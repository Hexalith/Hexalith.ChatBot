using Hexalith.ChatBot.UI.Design;

namespace Hexalith.ChatBot.UI.State.AssociationReview;

public sealed record AssociationCorrectionSubmitResult(
    string CommandId,
    string CorrelationId,
    string? TaskId,
    string LifecycleState,
    AssociationReviewModel Review);
