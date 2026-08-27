namespace Hexalith.ChatBot.Contracts.Queries;

/// <summary>
/// Cursor page metadata for tenant-scoped project conversation reads.
/// </summary>
public sealed record ProjectConversationCursorPage(
    string? NextCursor,
    bool HasMore,
    int PageSize)
{
    /// <summary>Gets stream-scoped authoritative coverage; an empty all-covering read is represented explicitly.</summary>
    public IReadOnlyList<ProjectConversationStreamCoverage> AuthoritativeCoverage { get; init; } = [];

    /// <summary>Gets a value indicating whether the empty page authoritatively covers all known conversation streams.</summary>
    public bool IsAllCoveringEmpty { get; init; }
}
