using Hexalith.ChatBot.Contracts.Commands;
using Hexalith.ChatBot.Contracts.Enums;

namespace Hexalith.ChatBot.Server.Projections;

public sealed record AssociationCandidateView(
    string TenantId,
    string AssociationId,
    string IntakeId,
    string SourceMailboxId,
    string SourceConversationId,
    string? SourceThreadId,
    string? ProjectId,
    string? ProjectDisplayName,
    LifecycleState LifecycleState,
    AssociationScoringOutcome Outcome,
    AssociationThresholdBand ThresholdBand,
    double ConfidenceScore,
    IReadOnlyList<AssociationCandidate> Candidates,
    IReadOnlyList<AssociationExclusion> Exclusions,
    string ThresholdPolicyVersion,
    string SchemaVersion,
    string SourceProvenance,
    string DerivationKernelVersion,
    string RedactionState,
    string RetentionClass,
    long SourceVersion,
    string CorrelationId,
    DateTimeOffset DetectedAt,
    DateTimeOffset LastUpdatedAt,
    AssociationDecisionKind? DecisionKind = null,
    string? DecisionActorId = null,
    string? DecisionActorType = null,
    DateTimeOffset? DecidedAt = null,
    string? DecisionNote = null,
    string? DecisionNoteRedactionState = null,
    string? SurfaceOrigin = null,
    string? PolicySnapshotVersion = null,
    AssociationCorrectionKind? CorrectionKind = null,
    string? PriorProjectId = null,
    string? CorrectedProjectId = null,
    string? PredecessorAssociationId = null,
    string? SupersedesAssociationId = null,
    string? SupersededByAssociationId = null,
    string? CorrectionRationale = null,
    string? CorrectionRationaleRedactionState = null,
    string? CorrectionActorId = null,
    string? CorrectionActorType = null,
    DateTimeOffset? CorrectedAt = null,
    string? DownstreamImpactStatus = null,
    string? CorrectionId = null,
    string? WorkflowInstanceId = null,
    IReadOnlyList<string>? RequiredStoreKeys = null,
    IReadOnlyList<string>? CompletedStoreKeys = null,
    IReadOnlyList<string>? FailedStoreKeys = null,
    int? PropagationProgressNumerator = null,
    int? PropagationProgressDenominator = null,
    DateTimeOffset? PropagationStartedAtUtc = null,
    DateTimeOffset? PropagationCompletedAtUtc = null,
    DateTimeOffset? PropagationEstimatedCompletionAtUtc = null,
    string? PropagationStatus = null,
    bool IsCorrectedContextStale = false,
    string? ResponsibleOwnerRole = null,
    string? SafeNextAction = null,
    MailboxExternalSenderPosture? ExternalSender = null,
    MailboxAuthenticityStrictnessPolicySnapshot? StrictnessPolicy = null,
    string? RoutingReason = null)
{
    public const string CurrentSchemaVersion = "chatbot.association-candidate-view.v1";
    public const string MailboxSourceProvenance = "m365-mailbox-intake";

    public static string KeyFor(string tenantId, string associationId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(tenantId);
        ArgumentException.ThrowIfNullOrWhiteSpace(associationId);
        return $"{tenantId}:association:{associationId}";
    }
}
