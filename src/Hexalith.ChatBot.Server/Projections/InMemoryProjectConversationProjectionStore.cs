using System.Collections.Concurrent;

namespace Hexalith.ChatBot.Server.Projections;

internal sealed class InMemoryProjectConversationProjectionStore : IProjectConversationProjectionStore
{
    private readonly ConcurrentDictionary<string, ProjectConversationItemView> _items = new(StringComparer.Ordinal);

    public Task UpsertAsync(ProjectConversationItemView item, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(item);
        cancellationToken.ThrowIfCancellationRequested();

        string key = ProjectConversationItemView.KeyFor(item.TenantId, item.ProjectId, item.ItemId);
        _items.AddOrUpdate(
            key,
            static (_, incoming) => incoming,
            static (_, existing, incoming) => ProjectConversationItemView.ShouldReplace(existing, incoming) ? incoming : existing,
            item);
        return Task.CompletedTask;
    }

    public Task<ProjectConversationPage> ReadPageAsync(
        string tenantId,
        string projectId,
        string? cursor,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (!ProjectConversationCursor.TryRead(cursor, tenantId, projectId, out DateTimeOffset cursorTime, out string? cursorItemId))
        {
            return Task.FromResult(new ProjectConversationPage([], null, false, pageSize));
        }

        string prefix = $"{tenantId}:project-conversation:{projectId}:";
        ProjectConversationItemView[] pageItems = _items
            .Where(pair => pair.Key.StartsWith(prefix, StringComparison.Ordinal))
            .Select(static pair => pair.Value)
            .OrderBy(static item => item.OccurredAt)
            .ThenBy(static item => item.ItemId, StringComparer.Ordinal)
            .Where(item => IsAfterCursor(item, cursorTime, cursorItemId))
            .Take(pageSize + 1)
            .ToArray();
        bool hasMore = pageItems.Length > pageSize;
        ProjectConversationItemView[] visible = pageItems.Take(pageSize).ToArray();
        string? nextCursor = hasMore && visible.Length > 0
            ? ProjectConversationCursor.Create(tenantId, projectId, visible[^1].OccurredAt, visible[^1].ItemId)
            : null;
        return Task.FromResult(new ProjectConversationPage(visible, nextCursor, hasMore, pageSize));
    }

    private static bool IsAfterCursor(ProjectConversationItemView item, DateTimeOffset cursorTime, string? cursorItemId)
        => cursorItemId is null ||
            item.OccurredAt > cursorTime ||
            (item.OccurredAt == cursorTime && string.CompareOrdinal(item.ItemId, cursorItemId) > 0);
}
