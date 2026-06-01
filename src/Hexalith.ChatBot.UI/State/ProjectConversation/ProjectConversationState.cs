namespace Hexalith.ChatBot.UI.State.ProjectConversation;

public sealed record ProjectConversationState(
    bool IsLoading,
    ProjectConversationModel? Conversation,
    string? ErrorCode,
    bool IsWhyPanelLoading = false,
    ProjectAssociationWhyPanelModel? WhyPanel = null,
    string? WhyPanelProjectId = null,
    string? WhyPanelAssociationId = null,
    string? WhyPanelErrorCode = null);
