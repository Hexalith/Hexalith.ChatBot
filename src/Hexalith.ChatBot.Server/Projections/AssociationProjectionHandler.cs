using Hexalith.ChatBot.Server.Audit;

namespace Hexalith.ChatBot.Server.Projections;

internal sealed class AssociationProjectionHandler
{
    private readonly IAssociationProjectionStore _store;
    private readonly ISystemClock _clock;
    private readonly IProjectConversationProjectionStore? _conversationStore;

    public AssociationProjectionHandler(
        IAssociationProjectionStore store,
        ISystemClock clock,
        IProjectConversationProjectionStore? conversationStore = null)
    {
        _store = store ?? throw new ArgumentNullException(nameof(store));
        _clock = clock ?? throw new ArgumentNullException(nameof(clock));
        _conversationStore = conversationStore;
    }

    public enum ProjectionOutcome
    {
        Applied,
        Ignored,
    }

    public async Task<ProjectionOutcome> HandleAsync(
        AssociationNotification notification,
        CancellationToken cancellationToken = default)
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

        PropagationProjectionState propagation = PropagationProjectionState.Merge(existing, notification);

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
            notification.CorrectionId ?? existing?.CorrectionId,
            notification.WorkflowInstanceId ?? existing?.WorkflowInstanceId,
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
            notification.ResponsibleOwnerRole ?? existing?.ResponsibleOwnerRole,
            notification.SafeNextAction ?? existing?.SafeNextAction);

        await _store.SaveAsync(view, cancellationToken).ConfigureAwait(false);
        if (_conversationStore is not null && ProjectConversationItemView.FromAssociation(view) is { } conversationItem)
        {
            await _conversationStore.UpsertAsync(conversationItem, cancellationToken).ConfigureAwait(false);
        }

        return ProjectionOutcome.Applied;
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
            string[] completed = MergeKeys(existing?.CompletedStoreKeys, notification.CompletedStoreKeys);
            string[] failed = MergeKeys(existing?.FailedStoreKeys, notification.FailedStoreKeys);
            int denominator = notification.PropagationProgressDenominator is > 0
                ? notification.PropagationProgressDenominator.Value
                : required.Length > 0
                    ? required.Length
                    : existing?.PropagationProgressDenominator ?? 0;
            int numerator = notification.PropagationProgressNumerator is > 0
                ? Math.Max(notification.PropagationProgressNumerator.Value, completed.Length)
                : completed.Length > 0
                    ? completed.Length
                    : existing?.PropagationProgressNumerator ?? 0;

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
