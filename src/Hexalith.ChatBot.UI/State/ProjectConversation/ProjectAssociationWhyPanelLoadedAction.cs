namespace Hexalith.ChatBot.UI.State.ProjectConversation;

public sealed record ProjectAssociationWhyPanelLoadedAction(
    string ProjectId,
    string AssociationId,
    ProjectAssociationWhyPanelModel Panel);
