namespace Hexalith.ChatBot.UI.State.ProjectConversation;

public sealed record ProjectConversationModel(
    string ProjectId,
    string ProjectDisplayName,
    string? TenantContext,
    string Status,
    string ConversationState,
    IReadOnlyList<ProjectConversationItemModel> Items,
    string? NextCursor,
    bool HasMore,
    int PageSize,
    string SourceProvenance,
    string RedactionState,
    string RetentionClass,
    string SchemaVersion,
    string CorrelationId,
    string SafeNextAction)
{
    public IReadOnlyList<ProjectConversationStreamCoverageModel> AuthoritativeCoverage { get; init; } = [];

    public bool IsAllCoveringEmpty { get; init; }

    public bool IsEmpty => Items.Count == 0;

    public bool IsBlockedOrStale => Status is "Blocked" or "Stale" or "Degraded";
}
