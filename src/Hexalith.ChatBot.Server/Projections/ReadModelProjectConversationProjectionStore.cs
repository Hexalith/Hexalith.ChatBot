using Hexalith.ChatBot.Server.Governance.Conversations;

using Hexalith.ChatBot.Contracts.Commands;
using Hexalith.ChatBot.Contracts.Enums;
using Hexalith.ChatBot.Contracts.Queries;
using Hexalith.ChatBot.Server.Audit;
using Hexalith.EventStore.Client.Projections;

namespace Hexalith.ChatBot.Server.Projections;

internal sealed class ReadModelProjectConversationProjectionStore(
    IReadModelStore store,
    IProjectConversationChangePublisher? changePublisher = null) : IProjectConversationProjectionStore
{
    private const string ProjectionType = nameof(ReadModelProjectConversationProjectionStore);
    private readonly IProjectConversationChangePublisher _changePublisher =
        changePublisher ?? NoOpProjectConversationChangePublisher.Instance;

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
        ProjectConversationItemView? existing = await GetAsync<ProjectConversationItemView>(itemKey, cancellationToken)
            .ConfigureAwait(false);
        if (existing is not null && !ProjectConversationItemView.ShouldReplace(existing, item))
        {
            return;
        }

        ProjectConversationIndex index = await GetIndexAsync(item.TenantId, item.ProjectId, cancellationToken).ConfigureAwait(false);
        string[] itemIds = index.ItemIds.Concat([item.ItemId]).Distinct(StringComparer.Ordinal).Order(StringComparer.Ordinal).ToArray();
        await UpsertTenantProjectIndexAsync(item.TenantId, item.ProjectId, cancellationToken).ConfigureAwait(false);
        ProjectConversationIntakeIndex intakeIndex = await GetIntakeIndexAsync(item.TenantId, item.IntakeId, cancellationToken).ConfigureAwait(false);
        ProjectConversationItemRef[] itemRefs = intakeIndex.Items
            .Concat([new ProjectConversationItemRef(item.ProjectId, item.ItemId)])
            .Distinct()
            .OrderBy(static item => item.ProjectId, StringComparer.Ordinal)
            .ThenBy(static item => item.ItemId, StringComparer.Ordinal)
            .ToArray();

        await SaveAsync(itemKey, item, cancellationToken)
            .ConfigureAwait(false);
        await SaveAsync(indexKey, new ProjectConversationIndex(item.TenantId, item.ProjectId, itemIds), cancellationToken).ConfigureAwait(false);
        await SaveAsync(IntakeIndexKeyFor(item.TenantId, item.IntakeId), new ProjectConversationIntakeIndex(item.TenantId, item.IntakeId, itemRefs), cancellationToken).ConfigureAwait(false);

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
        => await GetAsync<ProjectConversationSourceEmailView>(ProjectConversationSourceEmailView.KeyFor(tenantId, intakeId), cancellationToken).ConfigureAwait(false);

    public async Task UpsertSourceEmailAsync(ProjectConversationSourceEmailView source, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(source);
        string sourceKey = ProjectConversationSourceEmailView.KeyFor(source.TenantId, source.IntakeId);
        ProjectConversationSourceEmailView? existing = await GetSourceEmailAsync(source.TenantId, source.IntakeId, cancellationToken).ConfigureAwait(false);
        if (existing is not null && !ProjectConversationSourceEmailView.ShouldReplace(existing, source))
        {
            return;
        }

        await SaveAsync(sourceKey, source, cancellationToken)
            .ConfigureAwait(false);

        ProjectConversationIntakeIndex intakeIndex = await GetIntakeIndexAsync(source.TenantId, source.IntakeId, cancellationToken).ConfigureAwait(false);
        foreach (ProjectConversationItemRef itemRef in intakeIndex.Items)
        {
            string itemKey = ProjectConversationItemView.KeyFor(source.TenantId, itemRef.ProjectId, itemRef.ItemId);
            ProjectConversationItemView? item = await GetAsync<ProjectConversationItemView>(itemKey, cancellationToken)
                .ConfigureAwait(false);
            if (item is not null && ProjectConversationItemView.IsSourceEmailEnrichableKind(item.Kind))
            {
                await SaveAsync(itemKey, item.WithSourceEmail(source), cancellationToken)
                    .ConfigureAwait(false);
            }
        }
    }

    public async Task UpsertParticipantResolutionAsync(ParticipantResolutionView participant, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(participant);
        string participantKey = ParticipantResolutionView.KeyFor(participant.TenantId, participant.ResolutionId, participant.SourceParticipantId);
        ParticipantResolutionView? existing = await GetAsync<ParticipantResolutionView>(participantKey, cancellationToken)
            .ConfigureAwait(false);
        if (existing is not null && existing.SourceVersion > participant.SourceVersion)
        {
            return;
        }

        await SaveAsync(participantKey, participant, cancellationToken)
            .ConfigureAwait(false);

        ProjectConversationParticipantIndex participantIndex = await GetParticipantIndexAsync(participant.TenantId, participant.IntakeId, cancellationToken).ConfigureAwait(false);
        string[] participantKeys = participantIndex.ParticipantKeys
            .Concat([participantKey])
            .Distinct(StringComparer.Ordinal)
            .Order(StringComparer.Ordinal)
            .ToArray();
        await SaveAsync(ParticipantIndexKeyFor(participant.TenantId, participant.IntakeId), new ProjectConversationParticipantIndex(participant.TenantId, participant.IntakeId, participantKeys), cancellationToken).ConfigureAwait(false);

        ProjectConversationIntakeIndex intakeIndex = await GetIntakeIndexAsync(participant.TenantId, participant.IntakeId, cancellationToken).ConfigureAwait(false);
        foreach (ProjectConversationItemRef itemRef in intakeIndex.Items)
        {
            string itemKey = ProjectConversationItemView.KeyFor(participant.TenantId, itemRef.ProjectId, itemRef.ItemId);
            ProjectConversationItemView? association = await GetAsync<ProjectConversationItemView>(itemKey, cancellationToken)
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
        ProjectConversationAttachmentSetView? existing = await GetAsync<ProjectConversationAttachmentSetView>(attachmentKey, cancellationToken)
            .ConfigureAwait(false);
        if (existing is not null && !ProjectConversationAttachmentSetView.ShouldReplace(existing, attachments))
        {
            return;
        }

        await SaveAsync(attachmentKey, attachments, cancellationToken)
            .ConfigureAwait(false);

        ProjectConversationAttachmentIndex attachmentIndex = await GetAttachmentIndexAsync(attachments.TenantId, attachments.IntakeId, cancellationToken).ConfigureAwait(false);
        string[] attachmentKeys = attachmentIndex.AttachmentKeys
            .Concat([attachmentKey])
            .Distinct(StringComparer.Ordinal)
            .Order(StringComparer.Ordinal)
            .ToArray();
        await SaveAsync(AttachmentIndexKeyFor(attachments.TenantId, attachments.IntakeId), new ProjectConversationAttachmentIndex(attachments.TenantId, attachments.IntakeId, attachmentKeys), cancellationToken).ConfigureAwait(false);

        ProjectConversationIntakeIndex intakeIndex = await GetIntakeIndexAsync(attachments.TenantId, attachments.IntakeId, cancellationToken).ConfigureAwait(false);
        foreach (ProjectConversationItemRef itemRef in intakeIndex.Items)
        {
            string itemKey = ProjectConversationItemView.KeyFor(attachments.TenantId, itemRef.ProjectId, itemRef.ItemId);
            ProjectConversationItemView? association = await GetAsync<ProjectConversationItemView>(itemKey, cancellationToken)
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

        ProjectConversationAttachmentSetView? attachmentSet = await GetAsync<ProjectConversationAttachmentSetView>(ProjectConversationAttachmentSetView.KeyFor(tenantId, intakeId), cancellationToken).ConfigureAwait(false);
        if (attachmentSet is null)
        {
            return [];
        }

        ProjectConversationIntakeIndex intakeIndex = await GetIntakeIndexAsync(tenantId, intakeId, cancellationToken).ConfigureAwait(false);
        var candidates = new List<ProjectConversationAttachmentStorageCandidate>();
        foreach (ProjectConversationItemRef itemRef in intakeIndex.Items)
        {
            ProjectConversationItemView? association = await GetAsync<ProjectConversationItemView>(ProjectConversationItemView.KeyFor(tenantId, itemRef.ProjectId, itemRef.ItemId), cancellationToken).ConfigureAwait(false);
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
        ProjectConversationAttachmentSetView? existing = await GetAsync<ProjectConversationAttachmentSetView>(attachmentKey, cancellationToken)
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
        await SaveAsync(attachmentKey, updated, cancellationToken)
            .ConfigureAwait(false);

        ProjectConversationIntakeIndex intakeIndex = await GetIntakeIndexAsync(outcome.TenantId, outcome.IntakeId, cancellationToken).ConfigureAwait(false);
        foreach (ProjectConversationItemRef itemRef in intakeIndex.Items)
        {
            ProjectConversationItemView? association = await GetAsync<ProjectConversationItemView>(ProjectConversationItemView.KeyFor(outcome.TenantId, itemRef.ProjectId, itemRef.ItemId), cancellationToken).ConfigureAwait(false);
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
        ProjectConversationAttachmentSetView? existing = await GetAsync<ProjectConversationAttachmentSetView>(attachmentKey, cancellationToken)
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
        await SaveAsync(attachmentKey, updated, cancellationToken)
            .ConfigureAwait(false);

        ProjectConversationIntakeIndex intakeIndex = await GetIntakeIndexAsync(outcome.TenantId, outcome.IntakeId, cancellationToken).ConfigureAwait(false);
        foreach (ProjectConversationItemRef itemRef in intakeIndex.Items)
        {
            ProjectConversationItemView? association = await GetAsync<ProjectConversationItemView>(ProjectConversationItemView.KeyFor(outcome.TenantId, itemRef.ProjectId, itemRef.ItemId), cancellationToken).ConfigureAwait(false);
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
        await UpsertTenantProjectIndexAsync(approval.TenantId, approval.ProjectId, cancellationToken).ConfigureAwait(false);

        string approvalKey = ApprovalEventView.KeyFor(approval.TenantId, approval.ProjectId, approval.ApprovalId);
        ApprovalEventView? request = await GetApprovalRequestAsync(approvalKey, cancellationToken).ConfigureAwait(false);
        if (approval.EventKind is ApprovalEventKind.Request &&
            (request is null || approval.SourceVersion >= request.SourceVersion))
        {
            await SaveAsync(approvalKey, approval, cancellationToken)
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

        // SignalR carries only an advisory tenant-scoped nudge. The persisted read model remains authoritative, so
        // publish only after the progress outcome has been durably written and never put provider content on the
        // notification path.
        if (!string.IsNullOrWhiteSpace(outcome.AiResponseProgressState))
        {
            await _changePublisher
                .PublishProjectConversationChangedAsync(outcome.TenantId, cancellationToken)
                .ConfigureAwait(false);
        }
    }

    public async Task UpsertTaskIntentAsync(TaskIntentRecord record, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(record);
        await UpsertTenantProjectIndexAsync(record.TenantId, record.ProjectId, cancellationToken).ConfigureAwait(false);
        string stateKey = TaskIntentStateKeyFor(record.TenantId, record.ProjectId, record.TaskIntentId);
        TaskIntentRecord? existing = await GetAsync<TaskIntentRecord>(stateKey, cancellationToken)
            .ConfigureAwait(false);
        if (existing is not null && !ShouldReplaceTaskIntent(existing, record))
        {
            return;
        }

        await SaveAsync(stateKey, record, cancellationToken)
            .ConfigureAwait(false);

        ProjectConversationIndex index = await GetIndexAsync(record.TenantId, record.ProjectId, cancellationToken).ConfigureAwait(false);
        foreach (string itemId in index.ItemIds)
        {
            string itemKey = ProjectConversationItemView.KeyFor(record.TenantId, record.ProjectId, itemId);
            ProjectConversationItemView? item = await GetAsync<ProjectConversationItemView>(itemKey, cancellationToken)
                .ConfigureAwait(false);
            if (item is not null && ShouldAttachTaskIntent(item, record))
            {
                await SaveAsync(itemKey, item with { CapturedTaskIntent = record }, cancellationToken)
                    .ConfigureAwait(false);
            }
        }
    }

    public async Task UpsertAiActionProposalAsync(
        string tenantId,
        AiActionProposalRecord proposal,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(tenantId);
        ArgumentNullException.ThrowIfNull(proposal);
        if (string.IsNullOrWhiteSpace(proposal.AssociationId) ||
            proposal.EvidenceSnapshotSourceVersion is null)
        {
            return;
        }

        string proposalKey = ProposalStateKeyFor(tenantId, proposal.ProposalId);
        await SaveAsync(proposalKey, proposal, cancellationToken)
            .ConfigureAwait(false);

        string indexKey = ProposalAssociationIndexKeyFor(tenantId, proposal.AssociationId);
        ProjectConversationProposalAssociationIndex index = await GetProposalAssociationIndexAsync(tenantId, proposal.AssociationId, cancellationToken).ConfigureAwait(false);
        string[] proposalKeys = index.ProposalKeys
            .Concat([proposalKey])
            .Distinct(StringComparer.Ordinal)
            .Order(StringComparer.Ordinal)
            .ToArray();
        await SaveAsync(indexKey, new ProjectConversationProposalAssociationIndex(tenantId, proposal.AssociationId, proposalKeys), cancellationToken).ConfigureAwait(false);
    }

    public async Task UpsertProjectConversationMessageAsync(
        ProjectConversationMessageAppended message,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(message);
        ProjectConversationItemView item = ProjectConversationItemView.FromUserMessage(message);
        await UpsertAsync(item, cancellationToken).ConfigureAwait(false);
    }

    public async Task<IReadOnlyList<AiActionProposalRecord>> ReadAiActionProposalsForAssociationAsync(
        string tenantId,
        string associationId,
        long correctedSourceVersion,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(tenantId);
        ArgumentException.ThrowIfNullOrWhiteSpace(associationId);
        ProjectConversationProposalAssociationIndex index = await GetProposalAssociationIndexAsync(tenantId, associationId, cancellationToken).ConfigureAwait(false);
        var proposals = new List<AiActionProposalRecord>();
        foreach (string proposalKey in index.ProposalKeys)
        {
            AiActionProposalRecord? proposal = await GetAsync<AiActionProposalRecord>(proposalKey, cancellationToken)
                .ConfigureAwait(false);
            if (proposal is not null &&
                string.Equals(proposal.AssociationId, associationId, StringComparison.Ordinal) &&
                proposal.EvidenceSnapshotSourceVersion is > 0 &&
                proposal.EvidenceSnapshotSourceVersion <= correctedSourceVersion)
            {
                proposals.Add(proposal);
            }
        }

        return proposals.OrderBy(static proposal => proposal.ProposalId, StringComparer.Ordinal).ToArray();
    }

    public async Task<TaskIntentRecord?> GetTaskIntentAsync(
        string tenantId,
        string projectId,
        string taskIntentId,
        CancellationToken cancellationToken = default)
    {
        TaskIntentRecord? record = await GetAsync<TaskIntentRecord>(TaskIntentStateKeyFor(tenantId, projectId, taskIntentId), cancellationToken).ConfigureAwait(false);
        return record is not null &&
            string.Equals(record.TenantId, tenantId, StringComparison.Ordinal) &&
            string.Equals(record.ProjectId, projectId, StringComparison.Ordinal)
                ? record
                : null;
    }

    public async Task<ProjectConversationPage> ReadPageAsync(
        string tenantId,
        string projectId,
        ProjectConversationCursorPosition? cursorPosition,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        ProjectConversationIndex index = await GetIndexAsync(tenantId, projectId, cancellationToken).ConfigureAwait(false);
        List<ProjectConversationItemView> items = [];
        foreach (string itemId in index.ItemIds)
        {
            ProjectConversationItemView? item = await GetAsync<ProjectConversationItemView>(ProjectConversationItemView.KeyFor(tenantId, projectId, itemId), cancellationToken).ConfigureAwait(false);
            if (item is not null)
            {
                items.Add(item);
            }
        }

        ProjectConversationItemView? latest = ProjectConversationItemView.LatestOf(items);
        ProjectConversationItemView[] pageItems = items
            .OrderByDescending(static item => item.OccurredAt)
            .ThenByDescending(static item => item.ItemId, StringComparer.Ordinal)
            .Where(item => cursorPosition is null ||
                item.OccurredAt < cursorPosition.OccurredAt ||
                (item.OccurredAt == cursorPosition.OccurredAt && string.CompareOrdinal(item.ItemId, cursorPosition.ItemId) < 0))
            .Take(pageSize + 1)
            .ToArray();
        bool hasMore = pageItems.Length > pageSize;
        ProjectConversationItemView[] visible = pageItems.Take(pageSize).ToArray();
        ProjectConversationCursorPosition? nextCursorPosition = hasMore && visible.Length > 0
            ? new ProjectConversationCursorPosition(visible[^1].OccurredAt, visible[^1].ItemId)
            : null;
        return new ProjectConversationPage(visible, nextCursorPosition, hasMore, pageSize, latest);
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
            ProjectConversationItemView? item = await GetAsync<ProjectConversationItemView>(ProjectConversationItemView.KeyFor(tenantId, projectId, itemId), cancellationToken).ConfigureAwait(false);
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

    public async Task<IReadOnlyList<string>> EnumerateTenantIdsAsync(CancellationToken cancellationToken = default)
    {
        ProjectConversationTenantIndex index = await GetTenantIndexAsync(cancellationToken).ConfigureAwait(false);
        return index.TenantIds;
    }

    public async Task<IReadOnlyList<AdminQueueSummaryProjectionItem>> ReadOperationalQueueItemsAsync(
        string tenantId,
        DateTimeOffset nowUtc,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(tenantId);

        FixedClock clock = new(nowUtc);
        IReadOnlyList<ApprovalEventView> approvals = await ReadApprovalEventsAsync(tenantId, cancellationToken).ConfigureAwait(false);
        return approvals
            .Select(approval => ApprovalQueueItemBuilder.TryBuild(approval, ApprovalPriorityWeights.SafeDefaults, clock))
            .OfType<AdminQueueSummaryProjectionItem>()
            .OrderBy(static item => item.ItemRef, StringComparer.Ordinal)
            .ToArray();
    }

    public async Task<IReadOnlyList<ApprovalEventView>> ReadApprovalEventsAsync(
        string tenantId,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(tenantId);

        ProjectConversationTenantProjectIndex tenantIndex = await GetTenantProjectIndexAsync(tenantId, cancellationToken).ConfigureAwait(false);
        List<ApprovalEventView> approvals = [];
        foreach (string projectId in tenantIndex.ProjectIds)
        {
            ProjectConversationIndex projectIndex = await GetIndexAsync(tenantId, projectId, cancellationToken).ConfigureAwait(false);
            foreach (string itemId in projectIndex.ItemIds)
            {
                ApprovalEventView? approval = await GetAsync<ApprovalEventView>(ApprovalEventStateKeyFor(tenantId, projectId, itemId), cancellationToken).ConfigureAwait(false);
                if (approval is not null)
                {
                    approvals.Add(approval);
                }
            }
        }

        return approvals
            .OrderBy(static approval => approval.ProjectId, StringComparer.Ordinal)
            .ThenBy(static approval => approval.ApprovalId, StringComparer.Ordinal)
            .ThenBy(static approval => approval.SourceVersion)
            .ToArray();
    }

    private async Task<ApprovalEventView?> GetApprovalRequestAsync(string approvalKey, CancellationToken cancellationToken)
        => await GetAsync<ApprovalEventView>(approvalKey, cancellationToken)
            .ConfigureAwait(false);

    private async Task<ProjectConversationApprovalIndex> GetApprovalIndexAsync(
        string tenantId,
        string projectId,
        string approvalId,
        CancellationToken cancellationToken)
        => await GetAsync<ProjectConversationApprovalIndex>(ApprovalIndexKeyFor(tenantId, projectId, approvalId), cancellationToken).ConfigureAwait(false)
            ?? new ProjectConversationApprovalIndex(tenantId, projectId, approvalId, []);

    private async Task<ProjectConversationProposalAssociationIndex> GetProposalAssociationIndexAsync(
        string tenantId,
        string associationId,
        CancellationToken cancellationToken)
        => await GetAsync<ProjectConversationProposalAssociationIndex>(ProposalAssociationIndexKeyFor(tenantId, associationId), cancellationToken).ConfigureAwait(false)
            ?? new ProjectConversationProposalAssociationIndex(tenantId, associationId, []);

    private async Task UpsertMaterializedApprovalEventAsync(
        ApprovalEventView approval,
        CancellationToken cancellationToken)
    {
        string eventKey = ApprovalEventStateKeyFor(approval.TenantId, approval.ProjectId, approval.StableItemId);
        ApprovalEventView? existingEvent = await GetAsync<ApprovalEventView>(eventKey, cancellationToken)
            .ConfigureAwait(false);
        if (existingEvent is not null && existingEvent.SourceVersion > approval.SourceVersion)
        {
            return;
        }

        await SaveAsync(eventKey, approval, cancellationToken)
            .ConfigureAwait(false);

        ProjectConversationItemView item = ProjectConversationItemView.FromApprovalEvent(approval);
        string itemKey = ProjectConversationItemView.KeyFor(item.TenantId, item.ProjectId, item.ItemId);
        ProjectConversationItemView? existing = await GetAsync<ProjectConversationItemView>(itemKey, cancellationToken)
            .ConfigureAwait(false);
        if (existing is not null && !ProjectConversationItemView.ShouldReplace(existing, item))
        {
            return;
        }

        await SaveAsync(itemKey, item, cancellationToken)
            .ConfigureAwait(false);

        ProjectConversationIndex index = await GetIndexAsync(item.TenantId, item.ProjectId, cancellationToken).ConfigureAwait(false);
        string[] itemIds = index.ItemIds.Concat([item.ItemId]).Distinct(StringComparer.Ordinal).Order(StringComparer.Ordinal).ToArray();
        await SaveAsync(IndexKeyFor(item.TenantId, item.ProjectId), new ProjectConversationIndex(item.TenantId, item.ProjectId, itemIds), cancellationToken).ConfigureAwait(false);

        ProjectConversationApprovalIndex approvalIndex = await GetApprovalIndexAsync(item.TenantId, item.ProjectId, approval.ApprovalId, cancellationToken).ConfigureAwait(false);
        string[] approvalItemIds = approvalIndex.ItemIds
            .Concat([item.ItemId])
            .Distinct(StringComparer.Ordinal)
            .Order(StringComparer.Ordinal)
            .ToArray();
        await SaveAsync(ApprovalIndexKeyFor(item.TenantId, item.ProjectId, approval.ApprovalId), new ProjectConversationApprovalIndex(item.TenantId, item.ProjectId, approval.ApprovalId, approvalItemIds), cancellationToken).ConfigureAwait(false);
    }

    private async Task EnrichApprovalEventsWithRequestAsync(
        ApprovalEventView request,
        CancellationToken cancellationToken)
    {
        ProjectConversationApprovalIndex approvalIndex = await GetApprovalIndexAsync(request.TenantId, request.ProjectId, request.ApprovalId, cancellationToken).ConfigureAwait(false);
        foreach (string itemId in approvalIndex.ItemIds)
        {
            string eventKey = ApprovalEventStateKeyFor(request.TenantId, request.ProjectId, itemId);
            ApprovalEventView? existingEvent = await GetAsync<ApprovalEventView>(eventKey, cancellationToken)
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
        FailureStateEventView? existingEvent = await GetAsync<FailureStateEventView>(eventKey, cancellationToken)
            .ConfigureAwait(false);
        if (existingEvent is not null && existingEvent.SourceVersion > failure.SourceVersion)
        {
            return;
        }

        await SaveAsync(eventKey, failure, cancellationToken)
            .ConfigureAwait(false);

        ProjectConversationItemView item = ProjectConversationItemView.FromFailureStateEvent(failure);
        string itemKey = ProjectConversationItemView.KeyFor(item.TenantId, item.ProjectId, item.ItemId);
        ProjectConversationItemView? existing = await GetAsync<ProjectConversationItemView>(itemKey, cancellationToken)
            .ConfigureAwait(false);
        if (existing is not null && !ProjectConversationItemView.ShouldReplace(existing, item))
        {
            return;
        }

        await SaveAsync(itemKey, item, cancellationToken)
            .ConfigureAwait(false);

        ProjectConversationIndex index = await GetIndexAsync(item.TenantId, item.ProjectId, cancellationToken).ConfigureAwait(false);
        string[] itemIds = index.ItemIds.Concat([item.ItemId]).Distinct(StringComparer.Ordinal).Order(StringComparer.Ordinal).ToArray();
        await SaveAsync(IndexKeyFor(item.TenantId, item.ProjectId), new ProjectConversationIndex(item.TenantId, item.ProjectId, itemIds), cancellationToken).ConfigureAwait(false);
    }

    private async Task UpsertMaterializedAiOutcomeEventAsync(
        AiOutcomeEventView outcome,
        CancellationToken cancellationToken)
    {
        string eventKey = AiOutcomeEventStateKeyFor(outcome.TenantId, outcome.ProjectId, outcome.StableItemId);
        AiOutcomeEventView? existingEvent = await GetAsync<AiOutcomeEventView>(eventKey, cancellationToken)
            .ConfigureAwait(false);
        if (existingEvent is not null && existingEvent.SourceVersion > outcome.SourceVersion)
        {
            return;
        }

        await SaveAsync(eventKey, outcome, cancellationToken)
            .ConfigureAwait(false);

        ProjectConversationItemView item = ProjectConversationItemView.FromAiOutcomeEvent(outcome);
        string itemKey = ProjectConversationItemView.KeyFor(item.TenantId, item.ProjectId, item.ItemId);
        ProjectConversationItemView? existing = await GetAsync<ProjectConversationItemView>(itemKey, cancellationToken)
            .ConfigureAwait(false);
        if (existing is not null && !ProjectConversationItemView.ShouldReplace(existing, item))
        {
            return;
        }

        await SaveAsync(itemKey, item, cancellationToken)
            .ConfigureAwait(false);

        ProjectConversationIndex index = await GetIndexAsync(item.TenantId, item.ProjectId, cancellationToken).ConfigureAwait(false);
        string[] itemIds = index.ItemIds.Concat([item.ItemId]).Distinct(StringComparer.Ordinal).Order(StringComparer.Ordinal).ToArray();
        await SaveAsync(IndexKeyFor(item.TenantId, item.ProjectId), new ProjectConversationIndex(item.TenantId, item.ProjectId, itemIds), cancellationToken).ConfigureAwait(false);
    }

    private async Task<ProjectConversationIndex> GetIndexAsync(
        string tenantId,
        string projectId,
        CancellationToken cancellationToken)
        => await GetAsync<ProjectConversationIndex>(IndexKeyFor(tenantId, projectId), cancellationToken).ConfigureAwait(false)
            ?? new ProjectConversationIndex(tenantId, projectId, []);

    private sealed class FixedClock(DateTimeOffset now) : ISystemClock
    {
        public DateTimeOffset UtcNow { get; } = now.ToUniversalTime();
    }

    private async Task<ProjectConversationTenantIndex> GetTenantIndexAsync(CancellationToken cancellationToken)
        => await GetAsync<ProjectConversationTenantIndex>(TenantIndexKey(), cancellationToken)
            .ConfigureAwait(false)
            ?? new ProjectConversationTenantIndex([]);

    private async Task<ProjectConversationTenantProjectIndex> GetTenantProjectIndexAsync(
        string tenantId,
        CancellationToken cancellationToken)
        => await GetAsync<ProjectConversationTenantProjectIndex>(TenantProjectIndexKeyFor(tenantId), cancellationToken)
            .ConfigureAwait(false)
            ?? new ProjectConversationTenantProjectIndex(tenantId, []);

    private async Task UpsertTenantProjectIndexAsync(
        string tenantId,
        string projectId,
        CancellationToken cancellationToken)
    {
        ProjectConversationTenantIndex tenantIndex = await GetTenantIndexAsync(cancellationToken).ConfigureAwait(false);
        string[] tenantIds = tenantIndex.TenantIds
            .Concat([tenantId])
            .Distinct(StringComparer.Ordinal)
            .Order(StringComparer.Ordinal)
            .ToArray();
        await SaveAsync(TenantIndexKey(), new ProjectConversationTenantIndex(tenantIds), cancellationToken)
            .ConfigureAwait(false);

        ProjectConversationTenantProjectIndex projectIndex = await GetTenantProjectIndexAsync(tenantId, cancellationToken).ConfigureAwait(false);
        string[] projectIds = projectIndex.ProjectIds
            .Concat([projectId])
            .Distinct(StringComparer.Ordinal)
            .Order(StringComparer.Ordinal)
            .ToArray();
        await SaveAsync(TenantProjectIndexKeyFor(tenantId), new ProjectConversationTenantProjectIndex(tenantId, projectIds), cancellationToken).ConfigureAwait(false);
    }

    private async Task<ProjectConversationIntakeIndex> GetIntakeIndexAsync(
        string tenantId,
        string intakeId,
        CancellationToken cancellationToken)
        => await GetAsync<ProjectConversationIntakeIndex>(IntakeIndexKeyFor(tenantId, intakeId), cancellationToken).ConfigureAwait(false)
            ?? new ProjectConversationIntakeIndex(tenantId, intakeId, []);

    private async Task<ProjectConversationParticipantIndex> GetParticipantIndexAsync(
        string tenantId,
        string intakeId,
        CancellationToken cancellationToken)
        => await GetAsync<ProjectConversationParticipantIndex>(ParticipantIndexKeyFor(tenantId, intakeId), cancellationToken).ConfigureAwait(false)
            ?? new ProjectConversationParticipantIndex(tenantId, intakeId, []);

    private async Task<ProjectConversationAttachmentIndex> GetAttachmentIndexAsync(
        string tenantId,
        string intakeId,
        CancellationToken cancellationToken)
        => await GetAsync<ProjectConversationAttachmentIndex>(AttachmentIndexKeyFor(tenantId, intakeId), cancellationToken).ConfigureAwait(false)
            ?? new ProjectConversationAttachmentIndex(tenantId, intakeId, []);

    private async Task MaterializeParticipantsForAssociationAsync(
        ProjectConversationItemView association,
        CancellationToken cancellationToken)
    {
        ProjectConversationParticipantIndex participantIndex = await GetParticipantIndexAsync(association.TenantId, association.IntakeId, cancellationToken).ConfigureAwait(false);
        foreach (string participantKey in participantIndex.ParticipantKeys)
        {
            ParticipantResolutionView? participant = await GetAsync<ParticipantResolutionView>(participantKey, cancellationToken)
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
            ProjectConversationAttachmentSetView? attachmentSet = await GetAsync<ProjectConversationAttachmentSetView>(attachmentKey, cancellationToken)
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
        ProjectConversationItemView? existing = await GetAsync<ProjectConversationItemView>(itemKey, cancellationToken)
            .ConfigureAwait(false);
        if (existing is not null && !ProjectConversationItemView.ShouldReplace(existing, item))
        {
            return;
        }

        await SaveAsync(itemKey, item, cancellationToken)
            .ConfigureAwait(false);

        ProjectConversationIndex index = await GetIndexAsync(item.TenantId, item.ProjectId, cancellationToken).ConfigureAwait(false);
        string[] itemIds = index.ItemIds.Concat([item.ItemId]).Distinct(StringComparer.Ordinal).Order(StringComparer.Ordinal).ToArray();
        await SaveAsync(IndexKeyFor(item.TenantId, item.ProjectId), new ProjectConversationIndex(item.TenantId, item.ProjectId, itemIds), cancellationToken).ConfigureAwait(false);
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
            ProjectConversationItemView? existing = await GetAsync<ProjectConversationItemView>(itemKey, cancellationToken)
                .ConfigureAwait(false);
            if (existing is not null && !ProjectConversationItemView.ShouldReplace(existing, item))
            {
                continue;
            }

            await SaveAsync(itemKey, item, cancellationToken)
                .ConfigureAwait(false);

            ProjectConversationIndex index = await GetIndexAsync(item.TenantId, item.ProjectId, cancellationToken).ConfigureAwait(false);
            string[] itemIds = index.ItemIds.Concat([item.ItemId]).Distinct(StringComparer.Ordinal).Order(StringComparer.Ordinal).ToArray();
            await SaveAsync(IndexKeyFor(item.TenantId, item.ProjectId), new ProjectConversationIndex(item.TenantId, item.ProjectId, itemIds), cancellationToken).ConfigureAwait(false);
        }
    }

    private static string IndexKeyFor(string tenantId, string projectId)
        => $"{tenantId}:project-conversation:{projectId}:index";

    private static string TenantIndexKey()
        => "project-conversation:tenants:index";

    private static string TenantProjectIndexKeyFor(string tenantId)
        => $"{tenantId}:project-conversation:projects:index";

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

    private static string ProposalStateKeyFor(string tenantId, string proposalId)
        => $"{tenantId}:project-conversation:proposal:{proposalId}";

    private static string ProposalAssociationIndexKeyFor(string tenantId, string associationId)
        => $"{tenantId}:project-conversation:association:{associationId}:proposals";

    private static bool ShouldAttachTaskIntent(ProjectConversationItemView item, TaskIntentRecord record)
        => string.Equals(item.TenantId, record.TenantId, StringComparison.Ordinal) &&
            string.Equals(item.ProjectId, record.ProjectId, StringComparison.Ordinal) &&
            (string.Equals(item.ItemId, record.SourceMessageId, StringComparison.Ordinal) ||
                string.Equals(item.SourceProviderMessageId, record.SourceMessageId, StringComparison.Ordinal) ||
                string.Equals(item.AssociationId, record.SourceMessageId, StringComparison.Ordinal)) &&
            (item.CapturedTaskIntent is null || ShouldReplaceTaskIntent(item.CapturedTaskIntent, record));

    private static bool ShouldReplaceTaskIntent(TaskIntentRecord existing, TaskIntentRecord incoming)
        => incoming.SourceVersion > existing.SourceVersion ||
            incoming.SourceVersion == existing.SourceVersion &&
            TaskIntentStateRank(incoming.State) >= TaskIntentStateRank(existing.State);

    private static int TaskIntentStateRank(TaskIntentState state)
        => state switch
        {
            TaskIntentState.Captured => 0,
            TaskIntentState.Blocked or TaskIntentState.Rejected => 1,
            TaskIntentState.Converted or
                TaskIntentState.NotActionable or
                TaskIntentState.Duplicate or
                TaskIntentState.AlreadyHandled or
                TaskIntentState.OutOfScope => 2,
            _ => 0,
        };

    private async Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken)
        where T : class
        => (await store
            .GetAsync<T>(ChatBotReadModelStoreNames.StateStoreName, key, cancellationToken)
            .ConfigureAwait(false)).Value;

    private async Task<T> SaveAsync<T>(string key, T value, CancellationToken cancellationToken)
        where T : class
    {
        ArgumentNullException.ThrowIfNull(value);
        return await ReadModelWritePolicy
            .UpdateAsync<T>(
                store,
                ChatBotReadModelStoreNames.StateStoreName,
                key,
                _ => value,
                new ReadModelWriteContext(Category: typeof(T).Name, ProjectionType: ProjectionType),
                cancellationToken: cancellationToken)
            .ConfigureAwait(false);
    }

    private sealed record ProjectConversationIndex(
        string TenantId,
        string ProjectId,
        IReadOnlyList<string> ItemIds);

    private sealed record ProjectConversationTenantIndex(IReadOnlyList<string> TenantIds);

    private sealed record ProjectConversationTenantProjectIndex(
        string TenantId,
        IReadOnlyList<string> ProjectIds);

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

    private sealed record ProjectConversationProposalAssociationIndex(
        string TenantId,
        string AssociationId,
        IReadOnlyList<string> ProposalKeys);

    private sealed record ProjectConversationItemRef(string ProjectId, string ItemId);
}
