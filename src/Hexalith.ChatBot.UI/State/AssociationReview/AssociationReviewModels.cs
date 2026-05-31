using Hexalith.ChatBot.UI.Design;

namespace Hexalith.ChatBot.UI.State.AssociationReview;

public sealed record AssociationReviewModel(
    string AssociationId,
    string IntakeId,
    string SourceMailboxId,
    string SourceConversationId,
    string? SourceThreadId,
    string LifecycleState,
    string Outcome,
    string ThresholdBand,
    double ConfidenceScore,
    IReadOnlyList<string> ReasonCodes,
    IReadOnlyList<AssociationCandidateModel> Candidates,
    IReadOnlyList<AssociationEvidenceModel> Evidence,
    IReadOnlyList<string> DisabledActionReasonCodes,
    IReadOnlyList<string> NextActionReasonCodes,
    string ThresholdPolicyVersion,
    string KernelVersion,
    DateTimeOffset DetectedAt,
    string SourceProvenance,
    string RedactionState,
    string RetentionClass,
    string SchemaVersion,
    long SourceVersion,
    string CorrelationId,
    string? CorrectedProjectId = null,
    string? PriorProjectId = null,
    string? PredecessorAssociationId = null,
    string? SupersedesAssociationId = null,
    string? CorrectionRationale = null,
    string? DownstreamImpactStatus = null)
{
    public bool HasAuthorizedCandidates => Candidates.Count > 0;

    public bool IsTerminal => LifecycleState is "Associated" or "Rejected" or "Failed" or "Skipped";

    public bool IsStaleOrDegraded
        => ReasonCodes.Any(static code => code.Contains("unavailable", StringComparison.OrdinalIgnoreCase)
            || code.Contains("stale", StringComparison.OrdinalIgnoreCase)
            || code.Contains("scorer", StringComparison.OrdinalIgnoreCase));
}

public sealed record AssociationCandidateModel(
    string ProjectId,
    string DisplayLabel,
    double ConfidenceScore,
    int Rank,
    IReadOnlyList<string> ReasonCodes,
    IReadOnlyList<AssociationEvidenceModel> Evidence,
    bool RequiredEvidenceComplete);

public sealed record AssociationEvidenceModel(
    string Reference,
    string Fingerprint,
    string Kind,
    ChatBotEvidenceState State,
    string UnavailableReason);

public sealed record AssociationReviewActionModel(
    string Code,
    string Label,
    string Consequence,
    ChatBotGovernedActionState State,
    string DisabledReason);

public sealed record AssociationDecisionSubmitResult(
    string CommandId,
    string CorrelationId,
    string? TaskId,
    string LifecycleState,
    AssociationReviewModel Review);

public sealed record AssociationCorrectionSubmitResult(
    string CommandId,
    string CorrelationId,
    string? TaskId,
    string LifecycleState,
    AssociationReviewModel Review);
