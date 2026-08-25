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
    string? DownstreamImpactStatus = null,
    string? PropagationStatus = null,
    int? PropagationProgressNumerator = null,
    int? PropagationProgressDenominator = null,
    DateTimeOffset? PropagationEstimatedCompletionAtUtc = null,
    bool IsCorrectedContextStale = false,
    string? ResponsibleOwnerRole = null,
    string? SafeNextAction = null)
{
    public bool HasAuthorizedCandidates => Candidates.Count > 0;

    /// <summary>
    /// Gets a value indicating whether the association reached a terminal state, whether the outcome was a
    /// success or a failure. Action gating uses this; user-facing feedback must not, because a terminal
    /// success and a terminal failure are opposite messages. Use <see cref="IsTerminalSuccess"/> or
    /// <see cref="IsTerminalFailure"/> to choose feedback.
    /// </summary>
    public bool IsTerminal => IsTerminalSuccess || IsTerminalFailure;

    /// <summary>
    /// Gets a value indicating whether the association settled on a successful terminal outcome. These
    /// states are the goal of the review, not a policy failure.
    /// </summary>
    public bool IsTerminalSuccess => LifecycleState is "Associated" or "Corrected";

    /// <summary>
    /// Gets a value indicating whether the association settled on a terminal outcome that denied, failed, or
    /// skipped the association.
    /// </summary>
    public bool IsTerminalFailure => LifecycleState is "Rejected" or "Failed" or "Skipped";

    /// <summary>
    /// Gets a value indicating whether the association was deferred. Deferred is deliberately NOT terminal:
    /// the shipped consequence copy promises the item "remains visible for later review", so a deferred
    /// association stays re-decidable from this surface. Pinned by
    /// <c>AssociationReviewModelTests.DeferredIsNotTerminalSoTheItemStaysReDecidable</c>.
    /// </summary>
    public bool IsDeferred => LifecycleState is "Deferred";

    public bool IsPropagationBlocking => LifecycleState is "Correcting" or "Correction-delayed" || IsCorrectedContextStale;

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
