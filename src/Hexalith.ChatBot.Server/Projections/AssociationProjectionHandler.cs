using Hexalith.ChatBot.Server.Audit;

namespace Hexalith.ChatBot.Server.Projections;

internal sealed class AssociationProjectionHandler(
    IAssociationProjectionStore store,
    ISystemClock clock)
{
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

        AssociationCandidateView? existing = await store
            .GetAsync(notification.TenantId, notification.AssociationId, cancellationToken)
            .ConfigureAwait(false);
        if (existing is not null && existing.SourceVersion >= notification.SourceVersion)
        {
            return ProjectionOutcome.Ignored;
        }

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
            clock.UtcNow,
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
            notification.DownstreamImpactStatus);

        await store.SaveAsync(view, cancellationToken).ConfigureAwait(false);
        return ProjectionOutcome.Applied;
    }
}
