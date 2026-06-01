using Dapr.Client;

using Hexalith.ChatBot.Contracts.Enums;
using Hexalith.ChatBot.Contracts.Queries;

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

        if (ProjectConversationItemView.IsAssociationContextKind(item.Kind))
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
            if (association is not null && ProjectConversationItemView.IsAssociationContextKind(association.Kind))
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
            if (association is not null && ProjectConversationItemView.IsAssociationContextKind(association.Kind))
            {
                await UpsertMaterializedAttachmentsAsync(attachments, association, cancellationToken).ConfigureAwait(false);
            }
        }
    }

    public async Task<IReadOnlyList<ProjectConversationAttachmentStorageCandidate>> GetAttachmentStorageCandidatesAsync(
        string tenantId,
        string intakeId,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(tenantId);
        ArgumentException.ThrowIfNullOrWhiteSpace(intakeId);

        ProjectConversationAttachmentSetView? attachmentSet = await daprClient
            .GetStateAsync<ProjectConversationAttachmentSetView?>(
                DaprGovernedOperationViewStore.StateStoreName,
                ProjectConversationAttachmentSetView.KeyFor(tenantId, intakeId),
                cancellationToken: cancellationToken)
            .ConfigureAwait(false);
        if (attachmentSet is null)
        {
            return [];
        }

        ProjectConversationIntakeIndex intakeIndex = await GetIntakeIndexAsync(tenantId, intakeId, cancellationToken).ConfigureAwait(false);
        var candidates = new List<ProjectConversationAttachmentStorageCandidate>();
        foreach (ProjectConversationItemRef itemRef in intakeIndex.Items)
        {
            ProjectConversationItemView? association = await daprClient
                .GetStateAsync<ProjectConversationItemView?>(
                    DaprGovernedOperationViewStore.StateStoreName,
                    ProjectConversationItemView.KeyFor(tenantId, itemRef.ProjectId, itemRef.ItemId),
                    cancellationToken: cancellationToken)
                .ConfigureAwait(false);
            if (association is null ||
                !IsAttachmentStorageAssociationEligible(association))
            {
                continue;
            }

            candidates.AddRange(attachmentSet.Attachments
                .Where(static attachment => attachment.StorageStatus is ProjectConversationAttachmentStatus.Pending or ProjectConversationAttachmentStatus.Retryable)
                .Select(attachment => new ProjectConversationAttachmentStorageCandidate(
                    tenantId,
                    association.ProjectId,
                    association.AssociationId,
                    intakeId,
                    association.SourceMailboxId,
                    association.SourceProviderMessageId ?? attachment.IntakeId,
                    attachment.ProviderAttachmentId,
                    attachment.Ordinal,
                    attachment.SafeDisplayName,
                    attachment.ContentType,
                    attachment.SizeInBytes,
                    attachment.StorageStatus,
                    attachment.FolderId,
                    attachment.FileId,
                    attachment.RedactionState,
                    Math.Max(association.SourceVersion, attachment.SourceVersion),
                    string.IsNullOrWhiteSpace(association.CorrelationId) ? attachment.CorrelationId : association.CorrelationId)));
        }

        return candidates;
    }

    public async Task UpsertAttachmentStorageOutcomeAsync(
        ProjectConversationAttachmentStorageOutcomeView outcome,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(outcome);

        string attachmentKey = ProjectConversationAttachmentSetView.KeyFor(outcome.TenantId, outcome.IntakeId);
        ProjectConversationAttachmentSetView? existing = await daprClient
            .GetStateAsync<ProjectConversationAttachmentSetView?>(
                DaprGovernedOperationViewStore.StateStoreName,
                attachmentKey,
                cancellationToken: cancellationToken)
            .ConfigureAwait(false);
        if (existing is null)
        {
            return;
        }

        ProjectConversationAttachmentReferenceView[] updatedAttachments = existing.Attachments
            .Select(attachment => attachment.WithStorageOutcome(outcome))
            .ToArray();
        if (updatedAttachments.SequenceEqual(existing.Attachments))
        {
            return;
        }

        ProjectConversationAttachmentSetView updated = existing with
        {
            Attachments = updatedAttachments,
            SourceVersion = Math.Max(existing.SourceVersion, outcome.SourceVersion),
            CorrelationId = outcome.CorrelationId,
        };
        await daprClient
            .SaveStateAsync(DaprGovernedOperationViewStore.StateStoreName, attachmentKey, updated, cancellationToken: cancellationToken)
            .ConfigureAwait(false);

        ProjectConversationIntakeIndex intakeIndex = await GetIntakeIndexAsync(outcome.TenantId, outcome.IntakeId, cancellationToken).ConfigureAwait(false);
        foreach (ProjectConversationItemRef itemRef in intakeIndex.Items)
        {
            ProjectConversationItemView? association = await daprClient
                .GetStateAsync<ProjectConversationItemView?>(
                    DaprGovernedOperationViewStore.StateStoreName,
                    ProjectConversationItemView.KeyFor(outcome.TenantId, itemRef.ProjectId, itemRef.ItemId),
                    cancellationToken: cancellationToken)
                .ConfigureAwait(false);
            if (association is not null &&
                ProjectConversationItemView.IsAssociationContextKind(association.Kind) &&
                string.Equals(association.ProjectId, outcome.ProjectId, StringComparison.Ordinal) &&
                string.Equals(association.AssociationId, outcome.AssociationId, StringComparison.Ordinal))
            {
                await UpsertMaterializedAttachmentsAsync(updated, association, cancellationToken).ConfigureAwait(false);
            }
        }
    }

    public async Task UpsertAttachmentSafetyOutcomeAsync(
        ProjectConversationAttachmentSafetyOutcomeView outcome,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(outcome);

        string attachmentKey = ProjectConversationAttachmentSetView.KeyFor(outcome.TenantId, outcome.IntakeId);
        ProjectConversationAttachmentSetView? existing = await daprClient
            .GetStateAsync<ProjectConversationAttachmentSetView?>(
                DaprGovernedOperationViewStore.StateStoreName,
                attachmentKey,
                cancellationToken: cancellationToken)
            .ConfigureAwait(false);
        if (existing is null)
        {
            return;
        }

        ProjectConversationAttachmentReferenceView[] updatedAttachments = existing.Attachments
            .Select(attachment => attachment.WithSafetyOutcome(outcome))
            .ToArray();
        if (updatedAttachments.SequenceEqual(existing.Attachments))
        {
            return;
        }

        ProjectConversationAttachmentSetView updated = existing with
        {
            Attachments = updatedAttachments,
            SourceVersion = Math.Max(existing.SourceVersion, outcome.SourceVersion),
            CorrelationId = outcome.CorrelationId,
        };
        await daprClient
            .SaveStateAsync(DaprGovernedOperationViewStore.StateStoreName, attachmentKey, updated, cancellationToken: cancellationToken)
            .ConfigureAwait(false);

        ProjectConversationIntakeIndex intakeIndex = await GetIntakeIndexAsync(outcome.TenantId, outcome.IntakeId, cancellationToken).ConfigureAwait(false);
        foreach (ProjectConversationItemRef itemRef in intakeIndex.Items)
        {
            ProjectConversationItemView? association = await daprClient
                .GetStateAsync<ProjectConversationItemView?>(
                    DaprGovernedOperationViewStore.StateStoreName,
                    ProjectConversationItemView.KeyFor(outcome.TenantId, itemRef.ProjectId, itemRef.ItemId),
                    cancellationToken: cancellationToken)
                .ConfigureAwait(false);
            if (association is not null &&
                ProjectConversationItemView.IsAssociationContextKind(association.Kind) &&
                string.Equals(association.ProjectId, outcome.ProjectId, StringComparison.Ordinal) &&
                string.Equals(association.AssociationId, outcome.AssociationId, StringComparison.Ordinal))
            {
                await UpsertMaterializedAttachmentsAsync(updated, association, cancellationToken).ConfigureAwait(false);
            }
        }
    }

    public async Task UpsertApprovalEventAsync(ApprovalEventView approval, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(approval);

        string approvalKey = ApprovalEventView.KeyFor(approval.TenantId, approval.ProjectId, approval.ApprovalId);
        ApprovalEventView? request = await GetApprovalRequestAsync(approvalKey, cancellationToken).ConfigureAwait(false);
        if (approval.EventKind is ApprovalEventKind.Request &&
            (request is null || approval.SourceVersion >= request.SourceVersion))
        {
            await daprClient
                .SaveStateAsync(DaprGovernedOperationViewStore.StateStoreName, approvalKey, approval, cancellationToken: cancellationToken)
                .ConfigureAwait(false);
            request = approval;
        }

        if (request is not null)
        {
            approval = approval.WithRequestContext(request);
        }

        await UpsertMaterializedApprovalEventAsync(approval, cancellationToken).ConfigureAwait(false);

        if (approval.EventKind is ApprovalEventKind.Request && request is not null && Equals(request, approval))
        {
            await EnrichApprovalEventsWithRequestAsync(request, cancellationToken).ConfigureAwait(false);
        }
    }

    public async Task UpsertFailureStateEventAsync(FailureStateEventView failure, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(failure);
        await UpsertMaterializedFailureStateEventAsync(failure, cancellationToken).ConfigureAwait(false);
    }

    public async Task UpsertAiOutcomeEventAsync(AiOutcomeEventView outcome, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(outcome);
        await UpsertMaterializedAiOutcomeEventAsync(outcome, cancellationToken).ConfigureAwait(false);
    }

    public async Task UpsertTaskIntentAsync(TaskIntentRecord record, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(record);
        string stateKey = TaskIntentStateKeyFor(record.TenantId, record.ProjectId, record.TaskIntentId);
        TaskIntentRecord? existing = await daprClient
            .GetStateAsync<TaskIntentRecord?>(
                DaprGovernedOperationViewStore.StateStoreName,
                stateKey,
                cancellationToken: cancellationToken)
            .ConfigureAwait(false);
        if (existing is not null && existing.SourceVersion > record.SourceVersion)
        {
            return;
        }

        await daprClient
            .SaveStateAsync(DaprGovernedOperationViewStore.StateStoreName, stateKey, record, cancellationToken: cancellationToken)
            .ConfigureAwait(false);

        ProjectConversationIndex index = await GetIndexAsync(record.TenantId, record.ProjectId, cancellationToken).ConfigureAwait(false);
        foreach (string itemId in index.ItemIds)
        {
            string itemKey = ProjectConversationItemView.KeyFor(record.TenantId, record.ProjectId, itemId);
            ProjectConversationItemView? item = await daprClient
                .GetStateAsync<ProjectConversationItemView?>(
                    DaprGovernedOperationViewStore.StateStoreName,
                    itemKey,
                    cancellationToken: cancellationToken)
                .ConfigureAwait(false);
            if (item is not null && ShouldAttachTaskIntent(item, record))
            {
                await daprClient
                    .SaveStateAsync(
                        DaprGovernedOperationViewStore.StateStoreName,
                        itemKey,
                        item with { CapturedTaskIntent = record },
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

    public async Task<IReadOnlyList<ProjectConversationItemView>> ReadAiContextPackageItemsAsync(
        string tenantId,
        string projectId,
        CancellationToken cancellationToken = default)
    {
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

        return items
            .OrderBy(static item => item.OccurredAt)
            .ThenBy(static item => item.ItemId, StringComparer.Ordinal)
            .ToArray();
    }

    private async Task<ApprovalEventView?> GetApprovalRequestAsync(string approvalKey, CancellationToken cancellationToken)
        => await daprClient
            .GetStateAsync<ApprovalEventView?>(
                DaprGovernedOperationViewStore.StateStoreName,
                approvalKey,
                cancellationToken: cancellationToken)
            .ConfigureAwait(false);

    private async Task<ProjectConversationApprovalIndex> GetApprovalIndexAsync(
        string tenantId,
        string projectId,
        string approvalId,
        CancellationToken cancellationToken)
        => await daprClient
            .GetStateAsync<ProjectConversationApprovalIndex?>(
                DaprGovernedOperationViewStore.StateStoreName,
                ApprovalIndexKeyFor(tenantId, projectId, approvalId),
                cancellationToken: cancellationToken)
            .ConfigureAwait(false)
            ?? new ProjectConversationApprovalIndex(tenantId, projectId, approvalId, []);

    private async Task UpsertMaterializedApprovalEventAsync(
        ApprovalEventView approval,
        CancellationToken cancellationToken)
    {
        string eventKey = ApprovalEventStateKeyFor(approval.TenantId, approval.ProjectId, approval.StableItemId);
        ApprovalEventView? existingEvent = await daprClient
            .GetStateAsync<ApprovalEventView?>(
                DaprGovernedOperationViewStore.StateStoreName,
                eventKey,
                cancellationToken: cancellationToken)
            .ConfigureAwait(false);
        if (existingEvent is not null && existingEvent.SourceVersion > approval.SourceVersion)
        {
            return;
        }

        await daprClient
            .SaveStateAsync(DaprGovernedOperationViewStore.StateStoreName, eventKey, approval, cancellationToken: cancellationToken)
            .ConfigureAwait(false);

        ProjectConversationItemView item = ProjectConversationItemView.FromApprovalEvent(approval);
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

        ProjectConversationApprovalIndex approvalIndex = await GetApprovalIndexAsync(item.TenantId, item.ProjectId, approval.ApprovalId, cancellationToken).ConfigureAwait(false);
        string[] approvalItemIds = approvalIndex.ItemIds
            .Concat([item.ItemId])
            .Distinct(StringComparer.Ordinal)
            .Order(StringComparer.Ordinal)
            .ToArray();
        await daprClient
            .SaveStateAsync(
                DaprGovernedOperationViewStore.StateStoreName,
                ApprovalIndexKeyFor(item.TenantId, item.ProjectId, approval.ApprovalId),
                new ProjectConversationApprovalIndex(item.TenantId, item.ProjectId, approval.ApprovalId, approvalItemIds),
                cancellationToken: cancellationToken)
            .ConfigureAwait(false);
    }

    private async Task EnrichApprovalEventsWithRequestAsync(
        ApprovalEventView request,
        CancellationToken cancellationToken)
    {
        ProjectConversationApprovalIndex approvalIndex = await GetApprovalIndexAsync(request.TenantId, request.ProjectId, request.ApprovalId, cancellationToken).ConfigureAwait(false);
        foreach (string itemId in approvalIndex.ItemIds)
        {
            string eventKey = ApprovalEventStateKeyFor(request.TenantId, request.ProjectId, itemId);
            ApprovalEventView? existingEvent = await daprClient
                .GetStateAsync<ApprovalEventView?>(
                    DaprGovernedOperationViewStore.StateStoreName,
                    eventKey,
                    cancellationToken: cancellationToken)
                .ConfigureAwait(false);
            if (existingEvent is not null && existingEvent.EventKind is not ApprovalEventKind.Request)
            {
                await UpsertMaterializedApprovalEventAsync(existingEvent.WithRequestContext(request), cancellationToken).ConfigureAwait(false);
            }
        }
    }

    private async Task UpsertMaterializedFailureStateEventAsync(
        FailureStateEventView failure,
        CancellationToken cancellationToken)
    {
        string eventKey = FailureStateEventStateKeyFor(failure.TenantId, failure.ProjectId, failure.StableItemId);
        FailureStateEventView? existingEvent = await daprClient
            .GetStateAsync<FailureStateEventView?>(
                DaprGovernedOperationViewStore.StateStoreName,
                eventKey,
                cancellationToken: cancellationToken)
            .ConfigureAwait(false);
        if (existingEvent is not null && existingEvent.SourceVersion > failure.SourceVersion)
        {
            return;
        }

        await daprClient
            .SaveStateAsync(DaprGovernedOperationViewStore.StateStoreName, eventKey, failure, cancellationToken: cancellationToken)
            .ConfigureAwait(false);

        ProjectConversationItemView item = ProjectConversationItemView.FromFailureStateEvent(failure);
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

    private async Task UpsertMaterializedAiOutcomeEventAsync(
        AiOutcomeEventView outcome,
        CancellationToken cancellationToken)
    {
        string eventKey = AiOutcomeEventStateKeyFor(outcome.TenantId, outcome.ProjectId, outcome.StableItemId);
        AiOutcomeEventView? existingEvent = await daprClient
            .GetStateAsync<AiOutcomeEventView?>(
                DaprGovernedOperationViewStore.StateStoreName,
                eventKey,
                cancellationToken: cancellationToken)
            .ConfigureAwait(false);
        if (existingEvent is not null && existingEvent.SourceVersion > outcome.SourceVersion)
        {
            return;
        }

        await daprClient
            .SaveStateAsync(DaprGovernedOperationViewStore.StateStoreName, eventKey, outcome, cancellationToken: cancellationToken)
            .ConfigureAwait(false);

        ProjectConversationItemView item = ProjectConversationItemView.FromAiOutcomeEvent(outcome);
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

    private static bool IsAttachmentStorageAssociationEligible(ProjectConversationItemView item)
        => ProjectConversationItemView.IsAssociationContextKind(item.Kind) &&
            item.LifecycleState is LifecycleState.Associated &&
            string.IsNullOrWhiteSpace(item.SupersededByAssociationId) &&
            item.IsCorrectedContextStale is not true;

    private static string ParticipantIndexKeyFor(string tenantId, string intakeId)
        => $"{tenantId}:project-conversation:{intakeId}:participants";

    private static string AttachmentIndexKeyFor(string tenantId, string intakeId)
        => $"{tenantId}:project-conversation:{intakeId}:attachments";

    private static string ApprovalIndexKeyFor(string tenantId, string projectId, string approvalId)
        => $"{tenantId}:project-conversation:{projectId}:approval:{approvalId}:items";

    private static string ApprovalEventStateKeyFor(string tenantId, string projectId, string itemId)
        => $"{tenantId}:project-conversation:{projectId}:approval-event:{itemId}";

    private static string FailureStateEventStateKeyFor(string tenantId, string projectId, string itemId)
        => $"{tenantId}:project-conversation:{projectId}:failure-state:{itemId}";

    private static string AiOutcomeEventStateKeyFor(string tenantId, string projectId, string itemId)
        => $"{tenantId}:project-conversation:{projectId}:ai-outcome:{itemId}";

    private static string TaskIntentStateKeyFor(string tenantId, string projectId, string taskIntentId)
        => $"{tenantId}:project-conversation:{projectId}:task-intent:{taskIntentId}";

    private static bool ShouldAttachTaskIntent(ProjectConversationItemView item, TaskIntentRecord record)
        => string.Equals(item.TenantId, record.TenantId, StringComparison.Ordinal) &&
            string.Equals(item.ProjectId, record.ProjectId, StringComparison.Ordinal) &&
            (string.Equals(item.ItemId, record.SourceMessageId, StringComparison.Ordinal) ||
                string.Equals(item.SourceProviderMessageId, record.SourceMessageId, StringComparison.Ordinal) ||
                string.Equals(item.AssociationId, record.SourceMessageId, StringComparison.Ordinal)) &&
            (item.CapturedTaskIntent is null || record.SourceVersion >= item.CapturedTaskIntent.SourceVersion);

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

    private sealed record ProjectConversationApprovalIndex(
        string TenantId,
        string ProjectId,
        string ApprovalId,
        IReadOnlyList<string> ItemIds);

    private sealed record ProjectConversationItemRef(string ProjectId, string ItemId);
}
