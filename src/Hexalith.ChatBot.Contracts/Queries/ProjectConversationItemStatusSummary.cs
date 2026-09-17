using Hexalith.ChatBot.Contracts.Enums;

namespace Hexalith.ChatBot.Contracts.Queries;

public sealed record ProjectConversationItemStatusSummary(
    IReadOnlyList<ProjectConversationItemStatusFacet> Facets);
