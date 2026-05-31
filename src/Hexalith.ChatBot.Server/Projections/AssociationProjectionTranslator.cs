using Hexalith.ChatBot.Contracts.Commands;
using Hexalith.ChatBot.Contracts.Enums;
using Hexalith.ChatBot.Server.Association;
using Hexalith.ChatBot.Server.Operations;

namespace Hexalith.ChatBot.Server.Projections;

internal static class AssociationProjectionTranslator
{
    public static readonly string CandidatesGeneratedEventType = typeof(MailboxAssociationCandidatesGenerated).FullName!;
    public static readonly string AutoAssociatedEventType = typeof(MailboxEmailAssociatedToProject).FullName!;
    public static readonly string FailedClosedEventType = typeof(MailboxAssociationScoringFailedClosed).FullName!;
    public static readonly string DecisionConfirmedEventType = typeof(MailboxEmailAssociationConfirmed).FullName!;
    public static readonly string DecisionRejectedEventType = typeof(MailboxEmailAssociationRejected).FullName!;
    public static readonly string DecisionDeferredEventType = typeof(MailboxEmailAssociationDeferred).FullName!;
    public static readonly string DecisionNeedsReviewEventType = typeof(MailboxEmailAssociationMarkedNeedsReview).FullName!;
    public static readonly string CorrectionAcceptedEventType = typeof(MailboxEmailAssociationCorrected).FullName!;

    public static AssociationNotification? TryCreateNotification(PublishedAssociationEvent? published)
    {
        if (published is null ||
            !string.Equals(published.Domain, ChatBotEventStore.DomainName, StringComparison.Ordinal) ||
            string.IsNullOrWhiteSpace(published.TenantId) ||
            string.IsNullOrWhiteSpace(published.AggregateId) ||
            string.IsNullOrWhiteSpace(published.IntakeId) ||
            string.IsNullOrWhiteSpace(published.SourceMailboxId) ||
            string.IsNullOrWhiteSpace(published.SourceConversationId) ||
            string.IsNullOrWhiteSpace(published.ThresholdPolicyVersion) ||
            string.IsNullOrWhiteSpace(published.DerivationKernelVersion) ||
            string.IsNullOrWhiteSpace(published.RedactionState) ||
            string.IsNullOrWhiteSpace(published.RetentionClass) ||
            published.SequenceNumber <= 0)
        {
            return null;
        }

        AssociationScoringOutcome? outcome = OutcomeFor(published);
        if (outcome is null)
        {
            return null;
        }

        LifecycleState lifecycleState = LifecycleFor(published, outcome.Value);
        IReadOnlyList<AssociationCandidate> candidates = CandidatesFor(published);
        string? currentProjectId = string.Equals(published.EventTypeName, CorrectionAcceptedEventType, StringComparison.Ordinal)
            ? published.CorrectedProjectId ?? published.ProjectId
            : published.ProjectId;
        return new AssociationNotification(
            published.TenantId,
            published.AggregateId,
            published.IntakeId,
            published.SourceMailboxId,
            published.SourceConversationId,
            published.SourceThreadId,
            currentProjectId,
            published.ProjectDisplayName,
            lifecycleState,
            outcome.Value,
            published.ThresholdBand,
            published.ConfidenceScore,
            candidates,
            published.Exclusions ?? [],
            published.ThresholdPolicyVersion,
            published.DerivationKernelVersion,
            published.RedactionState,
            published.RetentionClass,
            published.SequenceNumber,
            published.DetectedAt == default ? published.Timestamp : published.DetectedAt,
            published.CorrelationId ?? string.Empty,
            published.DecisionKind,
            published.ActorId,
            published.ActorType,
            published.DecidedAt,
            published.DecisionNote,
            published.DecisionNoteRedactionState,
            published.SurfaceOrigin,
            published.PolicySnapshotVersion,
            published.CorrectionKind,
            published.PriorProjectId,
            published.CorrectedProjectId,
            published.PredecessorAssociationId,
            published.SupersedesAssociationId,
            published.SupersededByAssociationId,
            published.CorrectionRationale,
            published.CorrectionRationaleRedactionState,
            published.CorrectionActorId,
            published.CorrectionActorType,
            published.CorrectedAt,
            published.DownstreamImpactStatus);
    }

    private static AssociationScoringOutcome? OutcomeFor(PublishedAssociationEvent published)
    {
        if (string.Equals(published.EventTypeName, CandidatesGeneratedEventType, StringComparison.Ordinal))
        {
            return published.Outcome ?? AssociationScoringOutcome.CandidatesGenerated;
        }

        if (string.Equals(published.EventTypeName, AutoAssociatedEventType, StringComparison.Ordinal))
        {
            return AssociationScoringOutcome.AutoAssociated;
        }

        if (string.Equals(published.EventTypeName, FailedClosedEventType, StringComparison.Ordinal))
        {
            return AssociationScoringOutcome.FailedClosed;
        }

        if (IsDecisionEvent(published.EventTypeName))
        {
            return published.Outcome ?? AssociationScoringOutcome.CandidatesGenerated;
        }

        if (string.Equals(published.EventTypeName, CorrectionAcceptedEventType, StringComparison.Ordinal))
        {
            return published.Outcome ?? AssociationScoringOutcome.CandidatesGenerated;
        }

        return null;
    }

    private static LifecycleState LifecycleFor(PublishedAssociationEvent published, AssociationScoringOutcome outcome)
    {
        if (published.LifecycleState is { } lifecycleState)
        {
            return lifecycleState;
        }

        if (string.Equals(published.EventTypeName, DecisionConfirmedEventType, StringComparison.Ordinal))
        {
            return LifecycleState.Associated;
        }

        if (string.Equals(published.EventTypeName, DecisionRejectedEventType, StringComparison.Ordinal))
        {
            return LifecycleState.Rejected;
        }

        if (string.Equals(published.EventTypeName, DecisionDeferredEventType, StringComparison.Ordinal))
        {
            return LifecycleState.Deferred;
        }

        if (string.Equals(published.EventTypeName, DecisionNeedsReviewEventType, StringComparison.Ordinal))
        {
            return LifecycleState.NeedsReview;
        }

        if (string.Equals(published.EventTypeName, CorrectionAcceptedEventType, StringComparison.Ordinal))
        {
            return LifecycleState.Corrected;
        }

        return outcome == AssociationScoringOutcome.AutoAssociated
            ? LifecycleState.Associated
            : LifecycleState.NeedsReview;
    }

    private static bool IsDecisionEvent(string? eventTypeName)
        => string.Equals(eventTypeName, DecisionConfirmedEventType, StringComparison.Ordinal)
            || string.Equals(eventTypeName, DecisionRejectedEventType, StringComparison.Ordinal)
            || string.Equals(eventTypeName, DecisionDeferredEventType, StringComparison.Ordinal)
            || string.Equals(eventTypeName, DecisionNeedsReviewEventType, StringComparison.Ordinal);

    private static IReadOnlyList<AssociationCandidate> CandidatesFor(PublishedAssociationEvent published)
    {
        if (published.Candidates is { Count: > 0 })
        {
            return published.Candidates;
        }

        if (!IsDecisionEvent(published.EventTypeName) &&
            !string.Equals(published.EventTypeName, CorrectionAcceptedEventType, StringComparison.Ordinal))
        {
            return [];
        }

        IReadOnlyList<string> projectIds = published.CandidateProjectIds is { Count: > 0 }
            ? published.CandidateProjectIds
            : string.IsNullOrWhiteSpace(published.ProjectId) && string.IsNullOrWhiteSpace(published.CorrectedProjectId)
                ? []
                : [published.ProjectId ?? published.CorrectedProjectId!];
        if (projectIds.Count == 0)
        {
            return [];
        }

        IReadOnlyList<AssociationEvidenceReference> evidenceRefs = published.EvidenceRefs ?? [];
        IReadOnlyList<AssociationConfidenceInput> confidenceInputs = published.ConfidenceInputs ?? [];
        AssociationReasonCode reasonCode = published.DecisionKind switch
        {
            AssociationDecisionKind.Reject => AssociationReasonCode.NoAuthorizedCandidate,
            _ => AssociationReasonCode.ExplicitProjectIdentifierMatched,
        };

        return projectIds
            .Select((projectId, index) => new AssociationCandidate(
                projectId,
                string.Equals(projectId, published.ProjectId, StringComparison.Ordinal) ? published.ProjectDisplayName : null,
                published.ConfidenceScore,
                index + 1,
                [reasonCode],
                evidenceRefs,
                confidenceInputs,
                evidenceRefs.Count > 0))
            .ToArray();
    }
}
