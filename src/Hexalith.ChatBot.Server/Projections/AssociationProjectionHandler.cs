using Hexalith.ChatBot.Server.Audit;
using Hexalith.ChatBot.Server.Association.Intake;
using Hexalith.ChatBot.Server.Lifecycle.Attachments;
using Hexalith.ChatBot.Server.Operations;

namespace Hexalith.ChatBot.Server.Projections;

internal sealed class AssociationProjectionHandler
{
    private readonly IAssociationProjectionStore _store;
    private readonly ISystemClock _clock;
    private readonly IProjectConversationProjectionStore? _conversationStore;
    private readonly IAttachmentCaptureCoordinator? _attachmentCaptureCoordinator;
    private readonly IAiActionProposalInvalidationCoordinator? _aiActionProposalInvalidationCoordinator;

    public AssociationProjectionHandler(
        IAssociationProjectionStore store,
        ISystemClock clock,
        IProjectConversationProjectionStore? conversationStore = null,
        IAttachmentCaptureCoordinator? attachmentCaptureCoordinator = null,
        IAiActionProposalInvalidationCoordinator? aiActionProposalInvalidationCoordinator = null)
    {
        _store = store ?? throw new ArgumentNullException(nameof(store));
        _clock = clock ?? throw new ArgumentNullException(nameof(clock));
        _conversationStore = conversationStore;
        _attachmentCaptureCoordinator = attachmentCaptureCoordinator;
        _aiActionProposalInvalidationCoordinator = aiActionProposalInvalidationCoordinator;
    }

    public enum ProjectionOutcome
    {
        Applied,
        Ignored,
    }

    /// <summary>Creates a request-local fold so SDK replay can reconstruct earlier correction generations.</summary>
    internal AssociationProjectionHandler CreateReplay() => new(new InMemoryAssociationProjectionStore(), _clock);

    /// <summary>Restores scoring and correction evidence omitted by workflow progress events.</summary>
    internal async Task<AssociationNotification?> TranslateAsync(
        PublishedAssociationEvent published,
        bool requireConsecutive,
        CancellationToken cancellationToken,
        AssociationProjectionHandler? fallback = null)
    {
        AssociationNotification? complete = AssociationProjectionTranslator.TryCreateNotification(published);
        if (complete is not null || !AssociationProjectionTranslator.IsSparsePropagation(published)
            || published.Domain != ChatBotEventStore.DomainName || published.SequenceNumber <= 0
            || string.IsNullOrWhiteSpace(published.TenantId) || string.IsNullOrWhiteSpace(published.AggregateId)
            || string.IsNullOrWhiteSpace(published.IntakeId) || string.IsNullOrWhiteSpace(published.CorrectionId)
            || string.IsNullOrWhiteSpace(published.WorkflowInstanceId))
        {
            return complete;
        }

        AssociationCandidateView? existing = await _store.GetAsync(published.TenantId, published.AggregateId, cancellationToken)
            .ConfigureAwait(false);
        if (existing is null && fallback is not null)
        {
            existing = await fallback._store.GetAsync(published.TenantId, published.AggregateId, cancellationToken)
                .ConfigureAwait(false);
        }

        if (existing is null || (requireConsecutive && published.SequenceNumber - existing.SourceVersion > 1))
        {
            // A retry can see the prerequisite revision after the subscription or SDK projects it.
            throw new InvalidOperationException("The association correction projection has not reached the preceding revision.");
        }

        if (existing.IntakeId != published.IntakeId || existing.SourceMailboxId != published.SourceMailboxId
            || existing.SourceConversationId != published.SourceConversationId
            || existing.PriorProjectId is null || existing.PriorProjectId != published.PriorProjectId
            || existing.CorrectedProjectId is null || existing.CorrectedProjectId != published.CorrectedProjectId
            || (existing.CorrectionId is not null && existing.CorrectionId != published.CorrectionId)
            || (existing.WorkflowInstanceId is not null && existing.WorkflowInstanceId != published.WorkflowInstanceId))
        {
            return null;
        }

        return AssociationProjectionTranslator.TryCreateNotification(published with
        {
            SourceThreadId = existing.SourceThreadId,
            ProjectId = existing.ProjectId,
            ProjectDisplayName = existing.ProjectDisplayName,
            Candidates = existing.Candidates,
            Exclusions = existing.Exclusions,
            ConfidenceScore = existing.ConfidenceScore,
            ThresholdBand = existing.ThresholdBand,
            Outcome = existing.Outcome,
            ThresholdPolicyVersion = existing.ThresholdPolicyVersion,
            DerivationKernelVersion = existing.DerivationKernelVersion,
            DetectedAt = existing.DetectedAt,
            DecisionKind = existing.DecisionKind,
            ActorId = existing.DecisionActorId,
            ActorType = existing.DecisionActorType,
            DecidedAt = existing.DecidedAt,
            DecisionNote = existing.DecisionNote,
            DecisionNoteRedactionState = existing.DecisionNoteRedactionState,
            SurfaceOrigin = existing.SurfaceOrigin,
            PolicySnapshotVersion = existing.PolicySnapshotVersion,
            CorrectionKind = existing.CorrectionKind,
            PredecessorAssociationId = existing.PredecessorAssociationId,
            SupersedesAssociationId = existing.SupersedesAssociationId,
            SupersededByAssociationId = existing.SupersededByAssociationId,
            CorrectionRationale = existing.CorrectionRationale,
            CorrectionRationaleRedactionState = existing.CorrectionRationaleRedactionState,
            CorrectionActorId = existing.CorrectionActorId,
            CorrectionActorType = existing.CorrectionActorType,
            CorrectedAt = existing.CorrectedAt,
        });
    }

    public Task<ProjectionOutcome> HandleAsync(
        AssociationNotification notification,
        CancellationToken cancellationToken = default)
        => HandleAsync(notification, runSideEffects: true, cancellationToken);

    /// <summary>Updates association read models and optionally runs the owner projection's side effects.</summary>
    internal async Task<ProjectionOutcome> HandleAsync(
        AssociationNotification notification,
        bool runSideEffects,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(notification);

        AssociationCandidateView? existing = await _store
            .GetAsync(notification.TenantId, notification.AssociationId, cancellationToken)
            .ConfigureAwait(false);
        if (existing is not null && existing.SourceVersion > notification.SourceVersion)
        {
            return ProjectionOutcome.Ignored;
        }

        if (existing is not null &&
            existing.SourceVersion == notification.SourceVersion &&
            string.Equals(existing.PropagationStatus, Association.CorrectionPropagationStatuses.Complete, StringComparison.Ordinal) &&
            !string.Equals(notification.PropagationStatus, Association.CorrectionPropagationStatuses.Complete, StringComparison.Ordinal))
        {
            return ProjectionOutcome.Ignored;
        }

        bool startsNewCorrection = notification.PropagationStatus == Association.CorrectionPropagationStatuses.Pending
            && (existing is null || existing.SourceVersion < notification.SourceVersion);
        AssociationCandidateView? priorPropagation = startsNewCorrection ? null : existing;
        PropagationProjectionState propagation = PropagationProjectionState.Merge(priorPropagation, notification);

        AssociationCandidateView view = new(
            notification.TenantId,
            notification.AssociationId,
            notification.IntakeId,
            notification.SourceMailboxId,
            notification.SourceConversationId,
            notification.SourceThreadId,
            notification.ProjectId,
            notification.ProjectDisplayName,
            notification.LifecycleState,
            notification.Outcome,
            notification.ThresholdBand,
            notification.ConfidenceScore,
            notification.Candidates,
            notification.Exclusions,
            notification.ThresholdPolicyVersion,
            AssociationCandidateView.CurrentSchemaVersion,
            AssociationCandidateView.MailboxSourceProvenance,
            notification.DerivationKernelVersion,
            notification.RedactionState,
            notification.RetentionClass,
            notification.SourceVersion,
            notification.CorrelationId,
            notification.DetectedAt,
            _clock.UtcNow,
            notification.DecisionKind,
            notification.DecisionActorId,
            notification.DecisionActorType,
            notification.DecidedAt,
            notification.DecisionNote,
            notification.DecisionNoteRedactionState,
            notification.SurfaceOrigin,
            notification.PolicySnapshotVersion,
            notification.CorrectionKind,
            notification.PriorProjectId,
            notification.CorrectedProjectId,
            notification.PredecessorAssociationId,
            notification.SupersedesAssociationId,
            notification.SupersededByAssociationId,
            notification.CorrectionRationale,
            notification.CorrectionRationaleRedactionState,
            notification.CorrectionActorId,
            notification.CorrectionActorType,
            notification.CorrectedAt,
            notification.DownstreamImpactStatus ?? existing?.DownstreamImpactStatus,
            notification.CorrectionId ?? priorPropagation?.CorrectionId,
            notification.WorkflowInstanceId ?? priorPropagation?.WorkflowInstanceId,
            propagation.RequiredStoreKeys,
            propagation.CompletedStoreKeys,
            propagation.FailedStoreKeys,
            propagation.ProgressNumerator,
            propagation.ProgressDenominator,
            propagation.StartedAtUtc,
            propagation.CompletedAtUtc,
            propagation.EstimatedCompletionAtUtc,
            propagation.PropagationStatus,
            propagation.IsCorrectedContextStale,
            notification.ResponsibleOwnerRole ?? priorPropagation?.ResponsibleOwnerRole,
            notification.SafeNextAction ?? priorPropagation?.SafeNextAction,
            notification.ExternalSender ?? existing?.ExternalSender,
            notification.StrictnessPolicy ?? existing?.StrictnessPolicy,
            notification.RoutingReason ?? existing?.RoutingReason);

        await _store.SaveAsync(view, cancellationToken).ConfigureAwait(false);
        if (_conversationStore is not null)
        {
            ProjectConversationSourceEmailView? source = await _conversationStore
                .GetSourceEmailAsync(view.TenantId, view.IntakeId, cancellationToken)
                .ConfigureAwait(false);
            if (ProjectConversationItemView.FromAssociationSourceContext(view, source) is { } conversationItem)
            {
                await _conversationStore.UpsertAsync(conversationItem, cancellationToken).ConfigureAwait(false);
            }

            if (ProjectConversationItemView.FromAssociationDecision(view, source) is { } decisionItem)
            {
                await _conversationStore.UpsertAsync(decisionItem, cancellationToken).ConfigureAwait(false);
            }

            if (!string.IsNullOrWhiteSpace(view.PriorProjectId) &&
                !string.Equals(view.PriorProjectId, view.ProjectId, StringComparison.Ordinal))
            {
                AssociationCandidateView priorProjectView = view with
                {
                    ProjectId = view.PriorProjectId,
                    ProjectDisplayName = null,
                    CorrectedProjectId = null,
                };
                if (ProjectConversationItemView.FromAssociationSourceContext(priorProjectView, source) is { } priorSourceItem)
                {
                    await _conversationStore.UpsertAsync(priorSourceItem, cancellationToken).ConfigureAwait(false);
                }

                if (ProjectConversationItemView.FromAssociationDecision(priorProjectView, source) is { } priorDecisionItem)
                {
                    await _conversationStore.UpsertAsync(priorDecisionItem, cancellationToken).ConfigureAwait(false);
                }
            }

            if (runSideEffects)
            {
                await CaptureAttachmentsAsync(
                    view.TenantId,
                    view.IntakeId,
                    view.SourceVersion,
                    view.CorrelationId,
                    cancellationToken).ConfigureAwait(false);
            }
        }

        if (runSideEffects && _aiActionProposalInvalidationCoordinator is not null && !string.IsNullOrWhiteSpace(view.CorrectionId))
        {
            await _aiActionProposalInvalidationCoordinator
                .InvalidateAsync(view, cancellationToken)
                .ConfigureAwait(false);
        }

        return ProjectionOutcome.Applied;
    }

    public Task<ProjectionOutcome> HandleAsync(
        MailboxMessageIntakeCaptured captured,
        string tenantId,
        long sourceVersion,
        string correlationId,
        CancellationToken cancellationToken = default)
        => HandleAsync(captured, tenantId, sourceVersion, correlationId, runSideEffects: true, cancellationToken);

    /// <summary>Updates intake read models and optionally runs attachment capture.</summary>
    internal async Task<ProjectionOutcome> HandleAsync(
        MailboxMessageIntakeCaptured captured,
        string tenantId,
        long sourceVersion,
        string correlationId,
        bool runSideEffects,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(captured);
        ArgumentException.ThrowIfNullOrWhiteSpace(tenantId);
        if (_conversationStore is null ||
            sourceVersion <= 0 ||
            string.IsNullOrWhiteSpace(captured.IntakeId) ||
            string.IsNullOrWhiteSpace(captured.MailboxId) ||
            string.IsNullOrWhiteSpace(captured.ProviderMessageId) ||
            string.IsNullOrWhiteSpace(captured.ConversationId) ||
            string.IsNullOrWhiteSpace(captured.SourceProvenance) ||
            string.IsNullOrWhiteSpace(captured.RedactionState) ||
            string.IsNullOrWhiteSpace(captured.RetentionClass))
        {
            return ProjectionOutcome.Ignored;
        }

        ProjectConversationSourceEmailView source = ProjectConversationSourceEmailView.FromIntake(
            tenantId,
            captured,
            sourceVersion,
            correlationId);
        await _conversationStore.UpsertSourceEmailAsync(source, cancellationToken).ConfigureAwait(false);
        ProjectConversationAttachmentSetView attachments = ProjectConversationAttachmentSetView.FromIntake(
            tenantId,
            captured,
            sourceVersion,
            correlationId);
        await _conversationStore.UpsertAttachmentReferencesAsync(attachments, cancellationToken).ConfigureAwait(false);
        if (runSideEffects && captured.AttachmentReferences.Count > 0)
        {
            await CaptureAttachmentsAsync(
                tenantId,
                captured.IntakeId,
                sourceVersion,
                correlationId,
                cancellationToken).ConfigureAwait(false);
        }

        return ProjectionOutcome.Applied;
    }

    private async Task CaptureAttachmentsAsync(
        string tenantId,
        string intakeId,
        long sourceVersion,
        string correlationId,
        CancellationToken cancellationToken)
    {
        if (_attachmentCaptureCoordinator is null || sourceVersion <= 0)
        {
            return;
        }

        _ = await _attachmentCaptureCoordinator
            .CaptureAsync(
                new AttachmentCaptureCoordinatorRequest(
                    tenantId,
                    intakeId,
                    sourceVersion,
                    correlationId),
                cancellationToken)
            .ConfigureAwait(false);
    }

    private sealed record PropagationProjectionState(
        IReadOnlyList<string> RequiredStoreKeys,
        IReadOnlyList<string> CompletedStoreKeys,
        IReadOnlyList<string> FailedStoreKeys,
        int? ProgressNumerator,
        int? ProgressDenominator,
        DateTimeOffset? StartedAtUtc,
        DateTimeOffset? CompletedAtUtc,
        DateTimeOffset? EstimatedCompletionAtUtc,
        string? PropagationStatus,
        bool IsCorrectedContextStale)
    {
        public static PropagationProjectionState Merge(AssociationCandidateView? existing, AssociationNotification notification)
        {
            if (string.Equals(notification.PropagationStatus, Association.CorrectionPropagationStatuses.Complete, StringComparison.Ordinal))
            {
                string[] completedOnly = MergeKeys(null, notification.CompletedStoreKeys);
                string[] requiredOnly = MergeKeys(notification.RequiredStoreKeys, completedOnly);
                int denominatorOnly = notification.PropagationProgressDenominator is > 0
                    ? notification.PropagationProgressDenominator.Value
                    : requiredOnly.Length > 0
                        ? requiredOnly.Length
                        : completedOnly.Length;

                return new PropagationProjectionState(
                    requiredOnly,
                    completedOnly,
                    [],
                    denominatorOnly == 0 ? null : completedOnly.Length,
                    denominatorOnly == 0 ? null : denominatorOnly,
                    notification.PropagationStartedAtUtc ?? existing?.PropagationStartedAtUtc,
                    notification.PropagationCompletedAtUtc ?? existing?.PropagationCompletedAtUtc,
                    notification.PropagationEstimatedCompletionAtUtc ?? existing?.PropagationEstimatedCompletionAtUtc,
                    notification.PropagationStatus,
                    false);
            }

            string[] required = MergeKeys(existing?.RequiredStoreKeys, notification.RequiredStoreKeys);
            string[] completed = MergeKeys(existing?.CompletedStoreKeys, notification.CompletedStoreKeys)
                .Except(notification.FailedStoreKeys ?? [], StringComparer.Ordinal).ToArray();
            string[] failed = MergeKeys(existing?.FailedStoreKeys, notification.FailedStoreKeys)
                .Except(notification.CompletedStoreKeys ?? [], StringComparer.Ordinal).ToArray();
            int denominator = notification.PropagationProgressDenominator is > 0
                ? notification.PropagationProgressDenominator.Value
                : required.Length > 0
                    ? required.Length
                    : existing?.PropagationProgressDenominator ?? 0;
            int numerator = notification.PropagationProgressNumerator is > 0
                ? Math.Max(notification.PropagationProgressNumerator.Value, completed.Length)
                : completed.Length;

            return new PropagationProjectionState(
                required,
                completed,
                failed,
                denominator == 0 ? null : numerator,
                denominator == 0 ? null : denominator,
                notification.PropagationStartedAtUtc ?? existing?.PropagationStartedAtUtc,
                notification.PropagationCompletedAtUtc ?? existing?.PropagationCompletedAtUtc,
                notification.PropagationEstimatedCompletionAtUtc ?? existing?.PropagationEstimatedCompletionAtUtc,
                notification.PropagationStatus ?? existing?.PropagationStatus,
                string.Equals(notification.PropagationStatus, Hexalith.ChatBot.Server.Association.CorrectionPropagationStatuses.Complete, StringComparison.Ordinal)
                    ? false
                    : notification.IsCorrectedContextStale || (existing?.IsCorrectedContextStale ?? false));
        }

        private static string[] MergeKeys(IReadOnlyList<string>? existing, IReadOnlyList<string>? incoming)
            => (existing ?? [])
                .Concat(incoming ?? [])
                .Where(static key => !string.IsNullOrWhiteSpace(key))
                .Distinct(StringComparer.Ordinal)
                .Order(StringComparer.Ordinal)
                .ToArray();
    }
}
