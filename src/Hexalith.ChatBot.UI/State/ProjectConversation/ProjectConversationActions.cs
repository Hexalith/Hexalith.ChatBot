namespace Hexalith.ChatBot.UI.State.ProjectConversation;

public sealed record LoadProjectConversationAction(string ProjectId, string? Cursor = null);

public sealed record ProjectConversationLoadedAction(ProjectConversationModel Conversation);

public sealed record ProjectConversationFailedAction(string ErrorCode);

public sealed record SetProjectConversationComposerModeAction(ProjectConversationComposerMode Mode);

public sealed record SubmitProjectConversationComposerAction(
    string ProjectId,
    ProjectConversationComposerMode Mode,
    string Text,
    string Locale,
    long ExpectedSourceVersion);

public sealed record ProjectConversationComposerValidationFailedAction(string ErrorCode);

public sealed record ProjectConversationSubmissionAcceptedAction(ProjectConversationSubmissionReceiptModel Receipt);

public sealed record ProjectConversationSubmissionFailedAction(string ErrorCode);

public sealed record ProjectConversationAiResponseNudgeModel(
    string ProjectId,
    string ConversationId,
    string ResponseId,
    string GenerationId,
    string CorrelationId,
    long SourceVersion,
    long Sequence,
    string State,
    string RedactionState,
    string VisibilityState);

public sealed record ProjectConversationAiResponseNudgeReceivedAction(ProjectConversationAiResponseNudgeModel Nudge);

public sealed record ProjectConversationAiResponseNudgeRejectedAction(string ErrorCode);

public sealed record ProjectConversationAiResponseReconnectAction(string ProjectId);

public sealed record StopProjectConversationAiResponseAction(ProjectConversationAiResponseProgressModel Progress);

public sealed record ProjectConversationAiResponseCancellationPendingAction(string ResponseId, string GenerationId);

public sealed record ProjectConversationAiResponseCancellationAcceptedAction(ProjectConversationSubmissionReceiptModel Receipt);

public sealed record ProjectConversationAiResponseCancellationFailedAction(string ErrorCode);

public sealed record OpenProjectAssociationWhyPanelAction(string ProjectId, string AssociationId);

public sealed record ProjectAssociationWhyPanelLoadedAction(
    string ProjectId,
    string AssociationId,
    ProjectAssociationWhyPanelModel Panel);

public sealed record ProjectAssociationWhyPanelFailedAction(string ProjectId, string AssociationId, string ErrorCode);

public sealed record CloseProjectAssociationWhyPanelAction;
