using Dapr.Client;

using Hexalith.ChatBot.Contracts.Enums;

namespace Hexalith.ChatBot.Server.Projections;

internal sealed class DaprProjectConversationProjectionStore(DaprClient daprClient) : IProjectConversationProjectionStore
{
    public async Task UpsertAsync(ProjectConversationItemView item, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(item);
        ProjectConversationSourceEmailView? source = await GetSourceEmailAsync(item.TenantId, item.IntakeId, cancellationToken).ConfigureAwait(false);
        if (source is not null && ProjectConversationItemView.IsSourceEmailEnrichableKind(item.Kind))
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

        if (ProjectConversationItemView.IsSourceEmailEnrichableKind(item.Kind))
        {
            await MaterializeParticipantsForAssociationAsync(item, cancellationToken).ConfigureAwait(false);
            await MaterializeAttachmentsForAssociationAsync(item, cancellationToken).ConfigureAwait(false);
        }
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
            if (item is not null && ProjectConversationItemView.IsSourceEmailEnrichableKind(item.Kind))
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

    public async Task UpsertParticipantResolutionAsync(ParticipantResolutionView participant, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(participant);
        string participantKey = ParticipantResolutionView.KeyFor(participant.TenantId, participant.ResolutionId, participant.SourceParticipantId);
        ParticipantResolutionView? existing = await daprClient
            .GetStateAsync<ParticipantResolutionView?>(
                DaprGovernedOperationViewStore.StateStoreName,
                participantKey,
                cancellationToken: cancellationToken)
            .ConfigureAwait(false);
        if (existing is not null && existing.SourceVersion > participant.SourceVersion)
        {
            return;
        }

        await daprClient
            .SaveStateAsync(DaprGovernedOperationViewStore.StateStoreName, participantKey, participant, cancellationToken: cancellationToken)
            .ConfigureAwait(false);

        ProjectConversationParticipantIndex participantIndex = await GetParticipantIndexAsync(participant.TenantId, participant.IntakeId, cancellationToken).ConfigureAwait(false);
        string[] participantKeys = participantIndex.ParticipantKeys
            .Concat([participantKey])
            .Distinct(StringComparer.Ordinal)
            .Order(StringComparer.Ordinal)
            .ToArray();
        await daprClient
            .SaveStateAsync(
                DaprGovernedOperationViewStore.StateStoreName,
                ParticipantIndexKeyFor(participant.TenantId, participant.IntakeId),
                new ProjectConversationParticipantIndex(participant.TenantId, participant.IntakeId, participantKeys),
                cancellationToken: cancellationToken)
            .ConfigureAwait(false);

        ProjectConversationIntakeIndex intakeIndex = await GetIntakeIndexAsync(participant.TenantId, participant.IntakeId, cancellationToken).ConfigureAwait(false);
        foreach (ProjectConversationItemRef itemRef in intakeIndex.Items)
        {
            string itemKey = ProjectConversationItemView.KeyFor(participant.TenantId, itemRef.ProjectId, itemRef.ItemId);
            ProjectConversationItemView? association = await daprClient
                .GetStateAsync<ProjectConversationItemView?>(
                    DaprGovernedOperationViewStore.StateStoreName,
                    itemKey,
                    cancellationToken: cancellationToken)
                .ConfigureAwait(false);
            if (association is not null && ProjectConversationItemView.IsSourceEmailEnrichableKind(association.Kind))
            {
                await UpsertMaterializedParticipantAsync(participant, association, cancellationToken).ConfigureAwait(false);
            }
        }
    }

    public async Task UpsertAttachmentReferencesAsync(ProjectConversationAttachmentSetView attachments, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(attachments);
        string attachmentKey = ProjectConversationAttachmentSetView.KeyFor(attachments.TenantId, attachments.IntakeId);
        ProjectConversationAttachmentSetView? existing = await daprClient
            .GetStateAsync<ProjectConversationAttachmentSetView?>(
                DaprGovernedOperationViewStore.StateStoreName,
                attachmentKey,
                cancellationToken: cancellationToken)
            .ConfigureAwait(false);
        if (existing is not null && !ProjectConversationAttachmentSetView.ShouldReplace(existing, attachments))
        {
            return;
        }

        await daprClient
            .SaveStateAsync(DaprGovernedOperationViewStore.StateStoreName, attachmentKey, attachments, cancellationToken: cancellationToken)
            .ConfigureAwait(false);

        ProjectConversationAttachmentIndex attachmentIndex = await GetAttachmentIndexAsync(attachments.TenantId, attachments.IntakeId, cancellationToken).ConfigureAwait(false);
        string[] attachmentKeys = attachmentIndex.AttachmentKeys
            .Concat([attachmentKey])
            .Distinct(StringComparer.Ordinal)
            .Order(StringComparer.Ordinal)
            .ToArray();
        await daprClient
            .SaveStateAsync(
                DaprGovernedOperationViewStore.StateStoreName,
                AttachmentIndexKeyFor(attachments.TenantId, attachments.IntakeId),
                new ProjectConversationAttachmentIndex(attachments.TenantId, attachments.IntakeId, attachmentKeys),
                cancellationToken: cancellationToken)
            .ConfigureAwait(false);

        ProjectConversationIntakeIndex intakeIndex = await GetIntakeIndexAsync(attachments.TenantId, attachments.IntakeId, cancellationToken).ConfigureAwait(false);
        foreach (ProjectConversationItemRef itemRef in intakeIndex.Items)
        {
            string itemKey = ProjectConversationItemView.KeyFor(attachments.TenantId, itemRef.ProjectId, itemRef.ItemId);
            ProjectConversationItemView? association = await daprClient
                .GetStateAsync<ProjectConversationItemView?>(
                    DaprGovernedOperationViewStore.StateStoreName,
                    itemKey,
                    cancellationToken: cancellationToken)
                .ConfigureAwait(false);
            if (association is not null && ProjectConversationItemView.IsSourceEmailEnrichableKind(association.Kind))
            {
                await UpsertMaterializedAttachmentsAsync(attachments, association, cancellationToken).ConfigureAwait(false);
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

    private async Task<ProjectConversationParticipantIndex> GetParticipantIndexAsync(
        string tenantId,
        string intakeId,
        CancellationToken cancellationToken)
        => await daprClient
            .GetStateAsync<ProjectConversationParticipantIndex?>(
                DaprGovernedOperationViewStore.StateStoreName,
                ParticipantIndexKeyFor(tenantId, intakeId),
                cancellationToken: cancellationToken)
            .ConfigureAwait(false)
            ?? new ProjectConversationParticipantIndex(tenantId, intakeId, []);

    private async Task<ProjectConversationAttachmentIndex> GetAttachmentIndexAsync(
        string tenantId,
        string intakeId,
        CancellationToken cancellationToken)
        => await daprClient
            .GetStateAsync<ProjectConversationAttachmentIndex?>(
                DaprGovernedOperationViewStore.StateStoreName,
                AttachmentIndexKeyFor(tenantId, intakeId),
                cancellationToken: cancellationToken)
            .ConfigureAwait(false)
            ?? new ProjectConversationAttachmentIndex(tenantId, intakeId, []);

    private async Task MaterializeParticipantsForAssociationAsync(
        ProjectConversationItemView association,
        CancellationToken cancellationToken)
    {
        ProjectConversationParticipantIndex participantIndex = await GetParticipantIndexAsync(association.TenantId, association.IntakeId, cancellationToken).ConfigureAwait(false);
        foreach (string participantKey in participantIndex.ParticipantKeys)
        {
            ParticipantResolutionView? participant = await daprClient
                .GetStateAsync<ParticipantResolutionView?>(
                    DaprGovernedOperationViewStore.StateStoreName,
                    participantKey,
                    cancellationToken: cancellationToken)
                .ConfigureAwait(false);
            if (participant is not null)
            {
                await UpsertMaterializedParticipantAsync(participant, association, cancellationToken).ConfigureAwait(false);
            }
        }
    }

    private async Task MaterializeAttachmentsForAssociationAsync(
        ProjectConversationItemView association,
        CancellationToken cancellationToken)
    {
        ProjectConversationAttachmentIndex attachmentIndex = await GetAttachmentIndexAsync(association.TenantId, association.IntakeId, cancellationToken).ConfigureAwait(false);
        foreach (string attachmentKey in attachmentIndex.AttachmentKeys)
        {
            ProjectConversationAttachmentSetView? attachmentSet = await daprClient
                .GetStateAsync<ProjectConversationAttachmentSetView?>(
                    DaprGovernedOperationViewStore.StateStoreName,
                    attachmentKey,
                    cancellationToken: cancellationToken)
                .ConfigureAwait(false);
            if (attachmentSet is not null)
            {
                await UpsertMaterializedAttachmentsAsync(attachmentSet, association, cancellationToken).ConfigureAwait(false);
            }
        }
    }

    private async Task UpsertMaterializedParticipantAsync(
        ParticipantResolutionView participant,
        ProjectConversationItemView association,
        CancellationToken cancellationToken)
    {
        ProjectConversationItemView item = ProjectConversationItemView.FromParticipant(participant, association);
        string itemKey = ProjectConversationItemView.KeyFor(item.TenantId, item.ProjectId, item.ItemId);
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

        await daprClient
            .SaveStateAsync(DaprGovernedOperationViewStore.StateStoreName, itemKey, item, cancellationToken: cancellationToken)
            .ConfigureAwait(false);

        ProjectConversationIndex index = await GetIndexAsync(item.TenantId, item.ProjectId, cancellationToken).ConfigureAwait(false);
        string[] itemIds = index.ItemIds.Concat([item.ItemId]).Distinct(StringComparer.Ordinal).Order(StringComparer.Ordinal).ToArray();
        await daprClient
            .SaveStateAsync(
                DaprGovernedOperationViewStore.StateStoreName,
                IndexKeyFor(item.TenantId, item.ProjectId),
                new ProjectConversationIndex(item.TenantId, item.ProjectId, itemIds),
                cancellationToken: cancellationToken)
            .ConfigureAwait(false);
    }

    private async Task UpsertMaterializedAttachmentsAsync(
        ProjectConversationAttachmentSetView attachmentSet,
        ProjectConversationItemView association,
        CancellationToken cancellationToken)
    {
        foreach (ProjectConversationAttachmentReferenceView attachment in attachmentSet.Attachments)
        {
            ProjectConversationItemView item = ProjectConversationItemView.FromAttachment(attachment, association);
            string itemKey = ProjectConversationItemView.KeyFor(item.TenantId, item.ProjectId, item.ItemId);
            ProjectConversationItemView? existing = await daprClient
                .GetStateAsync<ProjectConversationItemView?>(
                    DaprGovernedOperationViewStore.StateStoreName,
                    itemKey,
                    cancellationToken: cancellationToken)
                .ConfigureAwait(false);
            if (existing is not null && !ProjectConversationItemView.ShouldReplace(existing, item))
            {
                continue;
            }

            await daprClient
                .SaveStateAsync(DaprGovernedOperationViewStore.StateStoreName, itemKey, item, cancellationToken: cancellationToken)
                .ConfigureAwait(false);

            ProjectConversationIndex index = await GetIndexAsync(item.TenantId, item.ProjectId, cancellationToken).ConfigureAwait(false);
            string[] itemIds = index.ItemIds.Concat([item.ItemId]).Distinct(StringComparer.Ordinal).Order(StringComparer.Ordinal).ToArray();
            await daprClient
                .SaveStateAsync(
                    DaprGovernedOperationViewStore.StateStoreName,
                    IndexKeyFor(item.TenantId, item.ProjectId),
                    new ProjectConversationIndex(item.TenantId, item.ProjectId, itemIds),
                    cancellationToken: cancellationToken)
                .ConfigureAwait(false);
        }
    }

    private static string IndexKeyFor(string tenantId, string projectId)
        => $"{tenantId}:project-conversation:{projectId}:index";

    private static string IntakeIndexKeyFor(string tenantId, string intakeId)
        => $"{tenantId}:project-conversation:{intakeId}:items";

    private static string ParticipantIndexKeyFor(string tenantId, string intakeId)
        => $"{tenantId}:project-conversation:{intakeId}:participants";

    private static string AttachmentIndexKeyFor(string tenantId, string intakeId)
        => $"{tenantId}:project-conversation:{intakeId}:attachments";

    private sealed record ProjectConversationIndex(
        string TenantId,
        string ProjectId,
        IReadOnlyList<string> ItemIds);

    private sealed record ProjectConversationIntakeIndex(
        string TenantId,
        string IntakeId,
        IReadOnlyList<ProjectConversationItemRef> Items);

    private sealed record ProjectConversationParticipantIndex(
        string TenantId,
        string IntakeId,
        IReadOnlyList<string> ParticipantKeys);

    private sealed record ProjectConversationAttachmentIndex(
        string TenantId,
        string IntakeId,
        IReadOnlyList<string> AttachmentKeys);

    private sealed record ProjectConversationItemRef(string ProjectId, string ItemId);
}
