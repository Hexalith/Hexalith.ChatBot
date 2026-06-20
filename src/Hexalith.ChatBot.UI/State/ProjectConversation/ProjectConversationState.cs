namespace Hexalith.ChatBot.UI.State.ProjectConversation;

public sealed record ProjectConversationState(
    bool IsLoading,
    ProjectConversationModel? Conversation,
    string? ErrorCode,
    ProjectConversationComposerMode ComposerMode = ProjectConversationComposerMode.Message,
    bool IsSubmitting = false,
    string? ComposerValidationErrorCode = null,
    string? SubmissionErrorCode = null,
    ProjectConversationSubmissionReceiptModel? PendingSubmission = null,
    bool IsWhyPanelLoading = false,
    ProjectAssociationWhyPanelModel? WhyPanel = null,
    string? WhyPanelProjectId = null,
    string? WhyPanelAssociationId = null,
    string? WhyPanelErrorCode = null,
    ProjectConversationAiResponseNudgeModel? LastAcceptedAiResponseNudge = null,
    string? StreamingErrorCode = null,
    string? StreamingNotice = null,
    bool IsCancellingAiResponse = false,
    string? CancellingResponseId = null,
    string? CancellingGenerationId = null);
