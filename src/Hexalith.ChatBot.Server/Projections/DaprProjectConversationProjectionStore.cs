using Dapr.Client;

namespace Hexalith.ChatBot.Server.Projections;

internal sealed class DaprProjectConversationProjectionStore(DaprClient daprClient) : IProjectConversationProjectionStore
{
    public async Task UpsertAsync(ProjectConversationItemView item, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(item);
        string itemKey = ProjectConversationItemView.KeyFor(item.TenantId, item.ProjectId, item.ItemId);
        string indexKey = IndexKeyFor(item.TenantId, item.ProjectId);
        ProjectConversationItemView? existing = await daprClient
            .GetStateAsync<ProjectConversationItemView?>(
                DaprGovernedOperationViewStore.StateStoreName,
                itemKey,
                cancellationToken: cancellationToken)
            .ConfigureAwait(false);
        if (existing is not null && !ProjectConversationItemView.ShouldReplace(existing, item))
        {
            return;
        }

        ProjectConversationIndex index = await GetIndexAsync(item.TenantId, item.ProjectId, cancellationToken).ConfigureAwait(false);
        string[] itemIds = index.ItemIds.Concat([item.ItemId]).Distinct(StringComparer.Ordinal).Order(StringComparer.Ordinal).ToArray();

        await daprClient
            .SaveStateAsync(DaprGovernedOperationViewStore.StateStoreName, itemKey, item, cancellationToken: cancellationToken)
            .ConfigureAwait(false);
        await daprClient
            .SaveStateAsync(
                DaprGovernedOperationViewStore.StateStoreName,
                indexKey,
                new ProjectConversationIndex(item.TenantId, item.ProjectId, itemIds),
                cancellationToken: cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task<ProjectConversationPage> ReadPageAsync(
        string tenantId,
        string projectId,
        string? cursor,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        if (!ProjectConversationCursor.TryRead(cursor, tenantId, projectId, out DateTimeOffset cursorTime, out string? cursorItemId))
        {
            return new ProjectConversationPage([], null, false, pageSize);
        }

        ProjectConversationIndex index = await GetIndexAsync(tenantId, projectId, cancellationToken).ConfigureAwait(false);
        List<ProjectConversationItemView> items = [];
        foreach (string itemId in index.ItemIds)
        {
            ProjectConversationItemView? item = await daprClient
                .GetStateAsync<ProjectConversationItemView?>(
                    DaprGovernedOperationViewStore.StateStoreName,
                    ProjectConversationItemView.KeyFor(tenantId, projectId, itemId),
                    cancellationToken: cancellationToken)
                .ConfigureAwait(false);
            if (item is not null)
            {
                items.Add(item);
            }
        }

        ProjectConversationItemView[] pageItems = items
            .OrderBy(static item => item.OccurredAt)
            .ThenBy(static item => item.ItemId, StringComparer.Ordinal)
            .Where(item => cursorItemId is null ||
                item.OccurredAt > cursorTime ||
                (item.OccurredAt == cursorTime && string.CompareOrdinal(item.ItemId, cursorItemId) > 0))
            .Take(pageSize + 1)
            .ToArray();
        bool hasMore = pageItems.Length > pageSize;
        ProjectConversationItemView[] visible = pageItems.Take(pageSize).ToArray();
        string? nextCursor = hasMore && visible.Length > 0
            ? ProjectConversationCursor.Create(tenantId, projectId, visible[^1].OccurredAt, visible[^1].ItemId)
            : null;
        return new ProjectConversationPage(visible, nextCursor, hasMore, pageSize);
    }

    private async Task<ProjectConversationIndex> GetIndexAsync(
        string tenantId,
        string projectId,
        CancellationToken cancellationToken)
        => await daprClient
            .GetStateAsync<ProjectConversationIndex?>(
                DaprGovernedOperationViewStore.StateStoreName,
                IndexKeyFor(tenantId, projectId),
                cancellationToken: cancellationToken)
            .ConfigureAwait(false)
            ?? new ProjectConversationIndex(tenantId, projectId, []);

    private static string IndexKeyFor(string tenantId, string projectId)
        => $"{tenantId}:project-conversation:{projectId}:index";

    private sealed record ProjectConversationIndex(
        string TenantId,
        string ProjectId,
        IReadOnlyList<string> ItemIds);
}
