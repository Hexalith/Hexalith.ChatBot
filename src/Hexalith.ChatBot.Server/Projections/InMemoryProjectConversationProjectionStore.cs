using System.Collections.Concurrent;

using Hexalith.ChatBot.Contracts.Enums;
using Hexalith.ChatBot.Contracts.Queries;

namespace Hexalith.ChatBot.Server.Projections;

internal sealed class InMemoryProjectConversationProjectionStore : IProjectConversationProjectionStore
{
    private readonly ConcurrentDictionary<string, ProjectConversationItemView> _items = new(StringComparer.Ordinal);
    private readonly ConcurrentDictionary<string, ProjectConversationSourceEmailView> _sourceEmails = new(StringComparer.Ordinal);
    private readonly ConcurrentDictionary<string, ParticipantResolutionView> _participants = new(StringComparer.Ordinal);
    private readonly ConcurrentDictionary<string, ProjectConversationAttachmentSetView> _attachments = new(StringComparer.Ordinal);
    private readonly ConcurrentDictionary<string, ApprovalEventView> _approvalRequests = new(StringComparer.Ordinal);
    private readonly ConcurrentDictionary<string, ApprovalEventView> _approvalEvents = new(StringComparer.Ordinal);
    private readonly ConcurrentDictionary<string, FailureStateEventView> _failureEvents = new(StringComparer.Ordinal);
    private readonly ConcurrentDictionary<string, AiOutcomeEventView> _aiOutcomeEvents = new(StringComparer.Ordinal);
    private readonly ConcurrentDictionary<string, AiActionProposalRecord> _aiActionProposals = new(StringComparer.Ordinal);
    private readonly ConcurrentDictionary<string, TaskIntentRecord> _taskIntents = new(StringComparer.Ordinal);
    private readonly ConcurrentDictionary<string, ConcurrentDictionary<string, byte>> _participantsByIntake = new(StringComparer.Ordinal);
    private readonly ConcurrentDictionary<string, ConcurrentDictionary<string, byte>> _attachmentsByIntake = new(StringComparer.Ordinal);
    private readonly ConcurrentDictionary<string, ConcurrentDictionary<string, byte>> _approvalItemsByApproval = new(StringComparer.Ordinal);
    private readonly ConcurrentDictionary<string, ConcurrentDictionary<string, byte>> _itemsByIntake = new(StringComparer.Ordinal);

    public Task UpsertAsync(ProjectConversationItemView item, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(item);
        cancellationToken.ThrowIfCancellationRequested();

        if (ProjectConversationItemView.IsSourceEmailEnrichableKind(item.Kind) &&
            _sourceEmails.TryGetValue(ProjectConversationSourceEmailView.KeyFor(item.TenantId, item.IntakeId), out ProjectConversationSourceEmailView? source))
        {
            item = item.WithSourceEmail(source);
        }

        string key = ProjectConversationItemView.KeyFor(item.TenantId, item.ProjectId, item.ItemId);
        _items.AddOrUpdate(
            key,
            static (_, incoming) => incoming,
            static (_, existing, incoming) => ProjectConversationItemView.ShouldReplace(existing, incoming) ? incoming : existing,
            item);
        _ = _itemsByIntake
            .GetOrAdd(IntakeIndexKeyFor(item.TenantId, item.IntakeId), static _ => new ConcurrentDictionary<string, byte>(StringComparer.Ordinal))
            .TryAdd(key, 0);
        if (ProjectConversationItemView.IsAssociationContextKind(item.Kind) &&
            _participantsByIntake.TryGetValue(IntakeIndexKeyFor(item.TenantId, item.IntakeId), out ConcurrentDictionary<string, byte>? participantKeys))
        {
            foreach (string participantKey in participantKeys.Keys)
            {
                if (_participants.TryGetValue(participantKey, out ParticipantResolutionView? participant))
                {
                    UpsertMaterializedParticipant(participant, item);
                }
            }
        }
        if (ProjectConversationItemView.IsAssociationContextKind(item.Kind) &&
            _attachmentsByIntake.TryGetValue(IntakeIndexKeyFor(item.TenantId, item.IntakeId), out ConcurrentDictionary<string, byte>? attachmentKeys))
        {
            foreach (string attachmentKey in attachmentKeys.Keys)
            {
                if (_attachments.TryGetValue(attachmentKey, out ProjectConversationAttachmentSetView? attachmentSet))
                {
                    UpsertMaterializedAttachments(attachmentSet, item);
                }
            }
        }

        return Task.CompletedTask;
    }

    public Task<ProjectConversationSourceEmailView?> GetSourceEmailAsync(
        string tenantId,
        string intakeId,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        _sourceEmails.TryGetValue(ProjectConversationSourceEmailView.KeyFor(tenantId, intakeId), out ProjectConversationSourceEmailView? source);
        return Task.FromResult(source);
    }

    public Task UpsertSourceEmailAsync(ProjectConversationSourceEmailView source, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(source);
        cancellationToken.ThrowIfCancellationRequested();

        string sourceKey = ProjectConversationSourceEmailView.KeyFor(source.TenantId, source.IntakeId);
        _sourceEmails.AddOrUpdate(
            sourceKey,
            static (_, incoming) => incoming,
            static (_, existing, incoming) => ProjectConversationSourceEmailView.ShouldReplace(existing, incoming) ? incoming : existing,
            source);
        if (!_sourceEmails.TryGetValue(sourceKey, out ProjectConversationSourceEmailView? effective) ||
            !Equals(effective, source))
        {
            return Task.CompletedTask;
        }

        if (_itemsByIntake.TryGetValue(IntakeIndexKeyFor(source.TenantId, source.IntakeId), out ConcurrentDictionary<string, byte>? itemKeys))
        {
            foreach (string itemKey in itemKeys.Keys)
            {
                _items.AddOrUpdate(
                    itemKey,
                    static (_, _) => throw new InvalidOperationException("Cannot enrich a missing conversation item."),
                    static (_, existing, incoming) => existing.WithSourceEmail(incoming),
                    source);
            }
        }

        return Task.CompletedTask;
    }

    public Task UpsertParticipantResolutionAsync(ParticipantResolutionView participant, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(participant);
        cancellationToken.ThrowIfCancellationRequested();

        string participantKey = ParticipantResolutionView.KeyFor(participant.TenantId, participant.ResolutionId, participant.SourceParticipantId);
        _participants.AddOrUpdate(
            participantKey,
            static (_, incoming) => incoming,
            static (_, existing, incoming) => incoming.SourceVersion >= existing.SourceVersion ? incoming : existing,
            participant);
        if (!_participants.TryGetValue(participantKey, out ParticipantResolutionView? effective) ||
            !Equals(effective, participant))
        {
            return Task.CompletedTask;
        }

        string intakeKey = IntakeIndexKeyFor(participant.TenantId, participant.IntakeId);
        _ = _participantsByIntake
            .GetOrAdd(intakeKey, static _ => new ConcurrentDictionary<string, byte>(StringComparer.Ordinal))
            .TryAdd(participantKey, 0);

        if (_itemsByIntake.TryGetValue(intakeKey, out ConcurrentDictionary<string, byte>? itemKeys))
        {
            foreach (string itemKey in itemKeys.Keys)
            {
                if (_items.TryGetValue(itemKey, out ProjectConversationItemView? association) &&
                    ProjectConversationItemView.IsAssociationContextKind(association.Kind))
                {
                    UpsertMaterializedParticipant(participant, association);
                }
            }
        }

        return Task.CompletedTask;
    }

    public Task UpsertAttachmentReferencesAsync(ProjectConversationAttachmentSetView attachments, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(attachments);
        cancellationToken.ThrowIfCancellationRequested();

        string attachmentKey = ProjectConversationAttachmentSetView.KeyFor(attachments.TenantId, attachments.IntakeId);
        _attachments.AddOrUpdate(
            attachmentKey,
            static (_, incoming) => incoming,
            static (_, existing, incoming) => ProjectConversationAttachmentSetView.ShouldReplace(existing, incoming) ? incoming : existing,
            attachments);
        if (!_attachments.TryGetValue(attachmentKey, out ProjectConversationAttachmentSetView? effective) ||
            !Equals(effective, attachments))
        {
            return Task.CompletedTask;
        }

        string intakeKey = IntakeIndexKeyFor(attachments.TenantId, attachments.IntakeId);
        _ = _attachmentsByIntake
            .GetOrAdd(intakeKey, static _ => new ConcurrentDictionary<string, byte>(StringComparer.Ordinal))
            .TryAdd(attachmentKey, 0);

        if (_itemsByIntake.TryGetValue(intakeKey, out ConcurrentDictionary<string, byte>? itemKeys))
        {
            foreach (string itemKey in itemKeys.Keys)
            {
                if (_items.TryGetValue(itemKey, out ProjectConversationItemView? association) &&
                    ProjectConversationItemView.IsAssociationContextKind(association.Kind))
                {
                    UpsertMaterializedAttachments(attachments, association);
                }
            }
        }

        return Task.CompletedTask;
    }

    public Task<IReadOnlyList<ProjectConversationAttachmentStorageCandidate>> GetAttachmentStorageCandidatesAsync(
        string tenantId,
        string intakeId,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(tenantId);
        ArgumentException.ThrowIfNullOrWhiteSpace(intakeId);
        cancellationToken.ThrowIfCancellationRequested();

        string intakeKey = IntakeIndexKeyFor(tenantId, intakeId);
        if (!_attachments.TryGetValue(ProjectConversationAttachmentSetView.KeyFor(tenantId, intakeId), out ProjectConversationAttachmentSetView? attachmentSet) ||
            !_itemsByIntake.TryGetValue(intakeKey, out ConcurrentDictionary<string, byte>? itemKeys))
        {
            return Task.FromResult<IReadOnlyList<ProjectConversationAttachmentStorageCandidate>>([]);
        }

        ProjectConversationItemView[] associations = itemKeys.Keys
            .Select(key => _items.TryGetValue(key, out ProjectConversationItemView? item) ? item : null)
            .OfType<ProjectConversationItemView>()
            .Where(IsAttachmentStorageAssociationEligible)
            .ToArray();

        ProjectConversationAttachmentStorageCandidate[] candidates = associations
            .SelectMany(association => attachmentSet.Attachments
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
                    string.IsNullOrWhiteSpace(association.CorrelationId) ? attachment.CorrelationId : association.CorrelationId)))
            .ToArray();

        return Task.FromResult<IReadOnlyList<ProjectConversationAttachmentStorageCandidate>>(candidates);
    }

    public Task UpsertAttachmentStorageOutcomeAsync(
        ProjectConversationAttachmentStorageOutcomeView outcome,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(outcome);
        cancellationToken.ThrowIfCancellationRequested();

        string attachmentKey = ProjectConversationAttachmentSetView.KeyFor(outcome.TenantId, outcome.IntakeId);
        if (!_attachments.TryGetValue(attachmentKey, out ProjectConversationAttachmentSetView? existing))
        {
            return Task.CompletedTask;
        }

        ProjectConversationAttachmentReferenceView[] updatedAttachments = existing.Attachments
            .Select(attachment => attachment.WithStorageOutcome(outcome))
            .ToArray();
        if (updatedAttachments.SequenceEqual(existing.Attachments))
        {
            return Task.CompletedTask;
        }

        ProjectConversationAttachmentSetView updated = existing with
        {
            Attachments = updatedAttachments,
            SourceVersion = Math.Max(existing.SourceVersion, outcome.SourceVersion),
            CorrelationId = outcome.CorrelationId,
        };
        _attachments[attachmentKey] = updated;

        string intakeKey = IntakeIndexKeyFor(outcome.TenantId, outcome.IntakeId);
        if (_itemsByIntake.TryGetValue(intakeKey, out ConcurrentDictionary<string, byte>? itemKeys))
        {
            foreach (string itemKey in itemKeys.Keys)
            {
                if (_items.TryGetValue(itemKey, out ProjectConversationItemView? association) &&
                    ProjectConversationItemView.IsAssociationContextKind(association.Kind) &&
                    string.Equals(association.ProjectId, outcome.ProjectId, StringComparison.Ordinal) &&
                    string.Equals(association.AssociationId, outcome.AssociationId, StringComparison.Ordinal))
                {
                    UpsertMaterializedAttachments(updated, association);
                }
            }
        }

        return Task.CompletedTask;
    }

    public Task UpsertAttachmentSafetyOutcomeAsync(
        ProjectConversationAttachmentSafetyOutcomeView outcome,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(outcome);
        cancellationToken.ThrowIfCancellationRequested();

        string attachmentKey = ProjectConversationAttachmentSetView.KeyFor(outcome.TenantId, outcome.IntakeId);
        if (!_attachments.TryGetValue(attachmentKey, out ProjectConversationAttachmentSetView? existing))
        {
            return Task.CompletedTask;
        }

        ProjectConversationAttachmentReferenceView[] updatedAttachments = existing.Attachments
            .Select(attachment => attachment.WithSafetyOutcome(outcome))
            .ToArray();
        if (updatedAttachments.SequenceEqual(existing.Attachments))
        {
            return Task.CompletedTask;
        }

        ProjectConversationAttachmentSetView updated = existing with
        {
            Attachments = updatedAttachments,
            SourceVersion = Math.Max(existing.SourceVersion, outcome.SourceVersion),
            CorrelationId = outcome.CorrelationId,
        };
        _attachments[attachmentKey] = updated;

        string intakeKey = IntakeIndexKeyFor(outcome.TenantId, outcome.IntakeId);
        if (_itemsByIntake.TryGetValue(intakeKey, out ConcurrentDictionary<string, byte>? itemKeys))
        {
            foreach (string itemKey in itemKeys.Keys)
            {
                if (_items.TryGetValue(itemKey, out ProjectConversationItemView? association) &&
                    ProjectConversationItemView.IsAssociationContextKind(association.Kind) &&
                    string.Equals(association.ProjectId, outcome.ProjectId, StringComparison.Ordinal) &&
                    string.Equals(association.AssociationId, outcome.AssociationId, StringComparison.Ordinal))
                {
                    UpsertMaterializedAttachments(updated, association);
                }
            }
        }

        return Task.CompletedTask;
    }

    public Task UpsertApprovalEventAsync(ApprovalEventView approval, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(approval);
        cancellationToken.ThrowIfCancellationRequested();

        string approvalKey = ApprovalEventView.KeyFor(approval.TenantId, approval.ProjectId, approval.ApprovalId);
        if (approval.EventKind is ApprovalEventKind.Request)
        {
            _approvalRequests.AddOrUpdate(
                approvalKey,
                static (_, incoming) => incoming,
                static (_, existing, incoming) => incoming.SourceVersion >= existing.SourceVersion ? incoming : existing,
                approval);
        }

        if (_approvalRequests.TryGetValue(approvalKey, out ApprovalEventView? request))
        {
            approval = approval.WithRequestContext(request);
        }

        UpsertMaterializedApprovalEvent(approval, approvalKey);
        if (approval.EventKind is ApprovalEventKind.Request &&
            _approvalItemsByApproval.TryGetValue(approvalKey, out ConcurrentDictionary<string, byte>? itemKeys))
        {
            foreach (string itemKey in itemKeys.Keys)
            {
                if (_approvalEvents.TryGetValue(itemKey, out ApprovalEventView? existing) &&
                    existing.EventKind is not ApprovalEventKind.Request)
                {
                    UpsertMaterializedApprovalEvent(existing.WithRequestContext(approval), approvalKey);
                }
            }
        }

        return Task.CompletedTask;
    }

    public Task UpsertFailureStateEventAsync(FailureStateEventView failure, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(failure);
        cancellationToken.ThrowIfCancellationRequested();

        UpsertMaterializedFailureStateEvent(failure);
        return Task.CompletedTask;
    }

    public Task UpsertAiOutcomeEventAsync(AiOutcomeEventView outcome, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(outcome);
        cancellationToken.ThrowIfCancellationRequested();

        UpsertMaterializedAiOutcomeEvent(outcome);
        return Task.CompletedTask;
    }

    public Task UpsertTaskIntentAsync(TaskIntentRecord record, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(record);
        cancellationToken.ThrowIfCancellationRequested();

        _taskIntents.AddOrUpdate(
            record.TaskIntentId,
            static (_, incoming) => incoming,
            static (_, existing, incoming) => ShouldReplaceTaskIntent(existing, incoming) ? incoming : existing,
            record);
        if (!_taskIntents.TryGetValue(record.TaskIntentId, out TaskIntentRecord? effective) ||
            !Equals(effective, record))
        {
            return Task.CompletedTask;
        }

        string prefix = $"{record.TenantId}:project-conversation:{record.ProjectId}:";
        foreach (string itemKey in _items.Keys.Where(key => key.StartsWith(prefix, StringComparison.Ordinal)).ToArray())
        {
            _items.AddOrUpdate(
                itemKey,
                static (_, _) => throw new InvalidOperationException("Cannot materialize task intent for a missing conversation item."),
                static (_, existing, incoming) => ShouldAttachTaskIntent(existing, incoming) ? existing with { CapturedTaskIntent = incoming } : existing,
                record);
        }

        return Task.CompletedTask;
    }

    public Task UpsertAiActionProposalAsync(
        string tenantId,
        AiActionProposalRecord proposal,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(tenantId);
        ArgumentNullException.ThrowIfNull(proposal);
        cancellationToken.ThrowIfCancellationRequested();

        if (string.IsNullOrWhiteSpace(proposal.AssociationId) ||
            proposal.EvidenceSnapshotSourceVersion is null)
        {
            return Task.CompletedTask;
        }

        _aiActionProposals[ProposalStateKeyFor(tenantId, proposal.ProposalId)] = proposal;
        return Task.CompletedTask;
    }

    public Task<IReadOnlyList<AiActionProposalRecord>> ReadAiActionProposalsForAssociationAsync(
        string tenantId,
        string associationId,
        long correctedSourceVersion,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(tenantId);
        ArgumentException.ThrowIfNullOrWhiteSpace(associationId);
        cancellationToken.ThrowIfCancellationRequested();

        IReadOnlyList<AiActionProposalRecord> proposals = _aiActionProposals
            .Where(item => item.Key.StartsWith($"{tenantId}:proposal:", StringComparison.Ordinal) &&
                string.Equals(item.Value.AssociationId, associationId, StringComparison.Ordinal) &&
                item.Value.EvidenceSnapshotSourceVersion is > 0 &&
                item.Value.EvidenceSnapshotSourceVersion <= correctedSourceVersion)
            .Select(static item => item.Value)
            .OrderBy(static proposal => proposal.ProposalId, StringComparer.Ordinal)
            .ToArray();
        return Task.FromResult(proposals);
    }

    public Task<TaskIntentRecord?> GetTaskIntentAsync(
        string tenantId,
        string projectId,
        string taskIntentId,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult(
            _taskIntents.TryGetValue(taskIntentId, out TaskIntentRecord? record) &&
            string.Equals(record.TenantId, tenantId, StringComparison.Ordinal) &&
            string.Equals(record.ProjectId, projectId, StringComparison.Ordinal)
                ? record
                : null);
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

    public Task<IReadOnlyList<ProjectConversationItemView>> ReadAiContextPackageItemsAsync(
        string tenantId,
        string projectId,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        string prefix = $"{tenantId}:project-conversation:{projectId}:";
        ProjectConversationItemView[] items = _items
            .Where(pair => pair.Key.StartsWith(prefix, StringComparison.Ordinal))
            .Select(static pair => pair.Value)
            .OrderBy(static item => item.OccurredAt)
            .ThenBy(static item => item.ItemId, StringComparer.Ordinal)
            .ToArray();
        return Task.FromResult<IReadOnlyList<ProjectConversationItemView>>(items);
    }

    private static bool IsAfterCursor(ProjectConversationItemView item, DateTimeOffset cursorTime, string? cursorItemId)
        => cursorItemId is null ||
            item.OccurredAt > cursorTime ||
            (item.OccurredAt == cursorTime && string.CompareOrdinal(item.ItemId, cursorItemId) > 0);

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

    private static string IntakeIndexKeyFor(string tenantId, string intakeId)
        => $"{tenantId}:project-conversation:{intakeId}:items";

    private static string ProposalStateKeyFor(string tenantId, string proposalId)
        => $"{tenantId}:proposal:{proposalId}";

    private static bool IsAttachmentStorageAssociationEligible(ProjectConversationItemView item)
        => ProjectConversationItemView.IsAssociationContextKind(item.Kind) &&
            item.LifecycleState is LifecycleState.Associated &&
            string.IsNullOrWhiteSpace(item.SupersededByAssociationId) &&
            item.IsCorrectedContextStale is not true;

    private void UpsertMaterializedParticipant(ParticipantResolutionView participant, ProjectConversationItemView association)
    {
        ProjectConversationItemView item = ProjectConversationItemView.FromParticipant(participant, association);
        string key = ProjectConversationItemView.KeyFor(item.TenantId, item.ProjectId, item.ItemId);
        _items.AddOrUpdate(
            key,
            static (_, incoming) => incoming,
            static (_, existing, incoming) => ProjectConversationItemView.ShouldReplace(existing, incoming) ? incoming : existing,
            item);
        _ = _itemsByIntake
            .GetOrAdd(IntakeIndexKeyFor(item.TenantId, item.IntakeId), static _ => new ConcurrentDictionary<string, byte>(StringComparer.Ordinal))
            .TryAdd(key, 0);
    }

    private void UpsertMaterializedAttachments(ProjectConversationAttachmentSetView attachmentSet, ProjectConversationItemView association)
    {
        foreach (ProjectConversationAttachmentReferenceView attachment in attachmentSet.Attachments)
        {
            ProjectConversationItemView item = ProjectConversationItemView.FromAttachment(attachment, association);
            string key = ProjectConversationItemView.KeyFor(item.TenantId, item.ProjectId, item.ItemId);
            _items.AddOrUpdate(
                key,
                static (_, incoming) => incoming,
                static (_, existing, incoming) => ProjectConversationItemView.ShouldReplace(existing, incoming) ? incoming : existing,
                item);
            _ = _itemsByIntake
                .GetOrAdd(IntakeIndexKeyFor(item.TenantId, item.IntakeId), static _ => new ConcurrentDictionary<string, byte>(StringComparer.Ordinal))
                .TryAdd(key, 0);
        }
    }

    private void UpsertMaterializedApprovalEvent(ApprovalEventView approval, string approvalKey)
    {
        string eventKey = ProjectConversationItemView.KeyFor(approval.TenantId, approval.ProjectId, approval.StableItemId);
        _approvalEvents.AddOrUpdate(
            eventKey,
            static (_, incoming) => incoming,
            static (_, existing, incoming) => incoming.SourceVersion >= existing.SourceVersion ? incoming : existing,
            approval);
        if (!_approvalEvents.TryGetValue(eventKey, out ApprovalEventView? effective) ||
            !Equals(effective, approval))
        {
            return;
        }

        ProjectConversationItemView item = ProjectConversationItemView.FromApprovalEvent(approval);
        _items.AddOrUpdate(
            eventKey,
            static (_, incoming) => incoming,
            static (_, existing, incoming) => ProjectConversationItemView.ShouldReplace(existing, incoming) ? incoming : existing,
            item);
        _ = _approvalItemsByApproval
            .GetOrAdd(approvalKey, static _ => new ConcurrentDictionary<string, byte>(StringComparer.Ordinal))
            .TryAdd(eventKey, 0);
    }

    private void UpsertMaterializedFailureStateEvent(FailureStateEventView failure)
    {
        string eventKey = FailureStateEventView.KeyFor(failure.TenantId, failure.ProjectId, failure.StableItemId);
        _failureEvents.AddOrUpdate(
            eventKey,
            static (_, incoming) => incoming,
            static (_, existing, incoming) => incoming.SourceVersion >= existing.SourceVersion ? incoming : existing,
            failure);
        if (!_failureEvents.TryGetValue(eventKey, out FailureStateEventView? effective) ||
            !Equals(effective, failure))
        {
            return;
        }

        ProjectConversationItemView item = ProjectConversationItemView.FromFailureStateEvent(failure);
        _items.AddOrUpdate(
            eventKey,
            static (_, incoming) => incoming,
            static (_, existing, incoming) => ProjectConversationItemView.ShouldReplace(existing, incoming) ? incoming : existing,
            item);
        _ = _itemsByIntake
            .GetOrAdd(IntakeIndexKeyFor(item.TenantId, item.IntakeId), static _ => new ConcurrentDictionary<string, byte>(StringComparer.Ordinal))
            .TryAdd(eventKey, 0);
    }

    private void UpsertMaterializedAiOutcomeEvent(AiOutcomeEventView outcome)
    {
        string eventKey = ProjectConversationItemView.KeyFor(outcome.TenantId, outcome.ProjectId, outcome.StableItemId);
        _aiOutcomeEvents.AddOrUpdate(
            eventKey,
            static (_, incoming) => incoming,
            static (_, existing, incoming) => incoming.SourceVersion >= existing.SourceVersion ? incoming : existing,
            outcome);
        if (!_aiOutcomeEvents.TryGetValue(eventKey, out AiOutcomeEventView? effective) ||
            !Equals(effective, outcome))
        {
            return;
        }

        ProjectConversationItemView item = ProjectConversationItemView.FromAiOutcomeEvent(outcome);
        _items.AddOrUpdate(
            eventKey,
            static (_, incoming) => incoming,
            static (_, existing, incoming) => ProjectConversationItemView.ShouldReplace(existing, incoming) ? incoming : existing,
            item);
        _ = _itemsByIntake
            .GetOrAdd(IntakeIndexKeyFor(item.TenantId, item.IntakeId), static _ => new ConcurrentDictionary<string, byte>(StringComparer.Ordinal))
            .TryAdd(eventKey, 0);
    }
}
