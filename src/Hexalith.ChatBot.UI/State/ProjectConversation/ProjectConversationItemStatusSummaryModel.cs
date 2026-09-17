namespace Hexalith.ChatBot.UI.State.ProjectConversation;

public sealed record ProjectConversationItemStatusSummaryModel(
    IReadOnlyList<ProjectConversationItemStatusFacetModel> Facets);
