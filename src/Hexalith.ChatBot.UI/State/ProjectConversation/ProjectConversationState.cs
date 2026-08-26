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
    string? CancellingGenerationId = null,

    // Set only when a typed read confirms a server-verified stop for a cancellation THIS session requested, so the
    // "Response stopped" announcement survives a Stop-control remount and is never raised for a historically stopped
    // response or another actor's stop. Cleared by the next composer submission or cancellation request. [AC4]
    string? VerifiedStopAnnouncementGenerationId = null,
    string? RequestedProjectId = null,
    string? CurrentLoadRequestId = null,
    string? HistoryLoadRequestId = null,
    bool IsHistoryLoading = false,
    IReadOnlyList<ProjectConversationItemModel>? HistoricalItems = null,
    IReadOnlySet<string>? CurrentPageItemIds = null);
