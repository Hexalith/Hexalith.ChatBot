using Dapr.Client;

namespace Hexalith.ChatBot.Server.Projections;

internal sealed class DaprProjectConversationProjectionStore(DaprClient daprClient) : IProjectConversationProjectionStore
{
    public async Task UpsertAsync(ProjectConversationItemView item, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(item);
        ProjectConversationSourceEmailView? source = await GetSourceEmailAsync(item.TenantId, item.IntakeId, cancellationToken).ConfigureAwait(false);
        if (source is not null)
        {
            item = item.WithSourceEmail(source);
        }

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
        ProjectConversationIntakeIndex intakeIndex = await GetIntakeIndexAsync(item.TenantId, item.IntakeId, cancellationToken).ConfigureAwait(false);
        ProjectConversationItemRef[] itemRefs = intakeIndex.Items
            .Concat([new ProjectConversationItemRef(item.ProjectId, item.ItemId)])
            .Distinct()
            .OrderBy(static item => item.ProjectId, StringComparer.Ordinal)
            .ThenBy(static item => item.ItemId, StringComparer.Ordinal)
            .ToArray();

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
        await daprClient
            .SaveStateAsync(
                DaprGovernedOperationViewStore.StateStoreName,
                IntakeIndexKeyFor(item.TenantId, item.IntakeId),
                new ProjectConversationIntakeIndex(item.TenantId, item.IntakeId, itemRefs),
                cancellationToken: cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task<ProjectConversationSourceEmailView?> GetSourceEmailAsync(
        string tenantId,
        string intakeId,
        CancellationToken cancellationToken = default)
        => await daprClient
            .GetStateAsync<ProjectConversationSourceEmailView?>(
                DaprGovernedOperationViewStore.StateStoreName,
                ProjectConversationSourceEmailView.KeyFor(tenantId, intakeId),
                cancellationToken: cancellationToken)
            .ConfigureAwait(false);

    public async Task UpsertSourceEmailAsync(ProjectConversationSourceEmailView source, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(source);
        string sourceKey = ProjectConversationSourceEmailView.KeyFor(source.TenantId, source.IntakeId);
        ProjectConversationSourceEmailView? existing = await GetSourceEmailAsync(source.TenantId, source.IntakeId, cancellationToken).ConfigureAwait(false);
        if (existing is not null && !ProjectConversationSourceEmailView.ShouldReplace(existing, source))
        {
            return;
        }

        await daprClient
            .SaveStateAsync(DaprGovernedOperationViewStore.StateStoreName, sourceKey, source, cancellationToken: cancellationToken)
            .ConfigureAwait(false);

        ProjectConversationIntakeIndex intakeIndex = await GetIntakeIndexAsync(source.TenantId, source.IntakeId, cancellationToken).ConfigureAwait(false);
        foreach (ProjectConversationItemRef itemRef in intakeIndex.Items)
        {
            string itemKey = ProjectConversationItemView.KeyFor(source.TenantId, itemRef.ProjectId, itemRef.ItemId);
            ProjectConversationItemView? item = await daprClient
                .GetStateAsync<ProjectConversationItemView?>(
                    DaprGovernedOperationViewStore.StateStoreName,
                    itemKey,
                    cancellationToken: cancellationToken)
                .ConfigureAwait(false);
            if (item is not null)
            {
                await daprClient
                    .SaveStateAsync(
                        DaprGovernedOperationViewStore.StateStoreName,
                        itemKey,
                        item.WithSourceEmail(source),
                        cancellationToken: cancellationToken)
                    .ConfigureAwait(false);
            }
        }
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

    private async Task<ProjectConversationIntakeIndex> GetIntakeIndexAsync(
        string tenantId,
        string intakeId,
        CancellationToken cancellationToken)
        => await daprClient
            .GetStateAsync<ProjectConversationIntakeIndex?>(
                DaprGovernedOperationViewStore.StateStoreName,
                IntakeIndexKeyFor(tenantId, intakeId),
                cancellationToken: cancellationToken)
            .ConfigureAwait(false)
            ?? new ProjectConversationIntakeIndex(tenantId, intakeId, []);

    private static string IndexKeyFor(string tenantId, string projectId)
        => $"{tenantId}:project-conversation:{projectId}:index";

    private static string IntakeIndexKeyFor(string tenantId, string intakeId)
        => $"{tenantId}:project-conversation-source-email:{intakeId}:items";

    private sealed record ProjectConversationIndex(
        string TenantId,
        string ProjectId,
        IReadOnlyList<string> ItemIds);

    private sealed record ProjectConversationIntakeIndex(
        string TenantId,
        string IntakeId,
        IReadOnlyList<ProjectConversationItemRef> Items);

    private sealed record ProjectConversationItemRef(string ProjectId, string ItemId);
}
