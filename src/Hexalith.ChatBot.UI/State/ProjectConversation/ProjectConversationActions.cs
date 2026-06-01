namespace Hexalith.ChatBot.UI.State.ProjectConversation;

public sealed record LoadProjectConversationAction(string ProjectId, string? Cursor = null);

public sealed record ProjectConversationLoadedAction(ProjectConversationModel Conversation);

public sealed record ProjectConversationFailedAction(string ErrorCode);

public sealed record OpenProjectAssociationWhyPanelAction(string ProjectId, string AssociationId);

public sealed record ProjectAssociationWhyPanelLoadedAction(
    string ProjectId,
    string AssociationId,
    ProjectAssociationWhyPanelModel Panel);

public sealed record ProjectAssociationWhyPanelFailedAction(string ProjectId, string AssociationId, string ErrorCode);

public sealed record CloseProjectAssociationWhyPanelAction;
