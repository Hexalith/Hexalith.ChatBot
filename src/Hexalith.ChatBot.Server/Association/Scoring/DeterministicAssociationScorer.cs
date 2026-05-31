using Hexalith.ChatBot.Contracts.Commands;
using Hexalith.ChatBot.Contracts.Enums;

namespace Hexalith.ChatBot.Server.Association.Scoring;

internal sealed class DeterministicAssociationScorer
{
    public const string CurrentKernelVersion = "association-deterministic.kernel.m0.v1";
    public const string ResultSchemaVersion = "chatbot.association-scoring-result.v1";
    public const string MetadataOnlyRedactionState = "metadata_only";
    public const string CollaborationRetentionClass = "collaboration_input";

    private static readonly AssociationSignalClass[] SignalPrecedence =
    [
        AssociationSignalClass.ExplicitProjectIdentifier,
        AssociationSignalClass.MailboxRoutingRule,
        AssociationSignalClass.ConversationThreadIdentifier,
    ];

    public static AssociationScoringComputation Score(AssociationScoringInput input)
    {
        ArgumentNullException.ThrowIfNull(input);

        AssociationThresholdPolicySnapshot policy = input.ThresholdPolicy;
        if (!AssociationThresholdPolicyValidator.IsValid(policy))
        {
            return FailedClosed(input, 0.0, [AssociationReasonCode.ScorerError]);
        }

        if (input.Signals.Any(static signal => !double.IsFinite(signal.Weight) || signal.Weight < 0.0))
        {
            return FailedClosed(input, 0.0, [AssociationReasonCode.ScorerError]);
        }

        if (HasConflictingRequiredEvidence(input.Signals))
        {
            return FailedClosed(input, 0.0, [AssociationReasonCode.ConflictingDeterministicEvidence]);
        }

        AssociationCandidate[] candidates = input.AuthorizedCandidates
            .Select(candidate => ScoreCandidate(candidate, input))
            .Where(static candidate => candidate.ConfidenceScore > 0.0)
            .OrderByDescending(static candidate => candidate.ConfidenceScore)
            .ThenByDescending(static candidate => candidate.RequiredEvidenceComplete)
            .ThenBy(static candidate => ReasonPrecedence(candidate.ReasonCodes))
            .ThenBy(static candidate => candidate.ProjectId, StringComparer.Ordinal)
            .Select(static (candidate, index) => candidate with { Rank = index + 1 })
            .ToArray();

        if (candidates.Length == 0)
        {
            AssociationScoringResult noCandidates = Result(
                input,
                0.0,
                AssociationThresholdBand.FailClosed,
                AssociationScoringOutcome.FailedClosed,
                [AssociationReasonCode.NoAuthorizedCandidate]);
            return new AssociationScoringComputation(noCandidates, [], input.Exclusions);
        }

        double topScore = candidates[0].ConfidenceScore;
        AssociationThresholdBand band = BandFor(topScore, policy);
        bool singleRequiredCandidate = candidates.Length == 1 && candidates[0].RequiredEvidenceComplete;
        AssociationScoringOutcome outcome = band == AssociationThresholdBand.Auto && singleRequiredCandidate
            ? AssociationScoringOutcome.AutoAssociated
            : AssociationScoringOutcome.CandidatesGenerated;
        AssociationReasonCode[] reasonCodes = outcome == AssociationScoringOutcome.AutoAssociated
            ? [.. candidates[0].ReasonCodes, AssociationReasonCode.RequiredEvidencePresent]
            : candidates.Length > 1
                ? [.. candidates[0].ReasonCodes, AssociationReasonCode.MultipleAuthorizedCandidates]
                : [.. candidates[0].ReasonCodes, AssociationReasonCode.MissingRequiredEvidence];

        return new AssociationScoringComputation(
            Result(input, topScore, band, outcome, DistinctReasons(reasonCodes)),
            candidates,
            input.Exclusions);
    }

    private static AssociationCandidate ScoreCandidate(ProjectAssociationCandidateEvidence candidate, AssociationScoringInput input)
    {
        AssociationDeterministicSignal[] candidateSignals = candidate.Signals
            .Where(signal => string.Equals(signal.ProjectId, candidate.ProjectId, StringComparison.Ordinal))
            .ToArray();
        AssociationReasonCode[] reasons = DistinctReasons(candidateSignals.Select(ReasonForSignal));
        AssociationEvidenceReference[] evidence = candidateSignals
            .OrderBy(static signal => Array.IndexOf(SignalPrecedence, signal.SignalClass))
            .Select(static signal => new AssociationEvidenceReference(
                signal.EvidenceReference,
                signal.EvidenceFingerprint,
                signal.SignalClass.ToString()))
            .ToArray();
        AssociationConfidenceInput[] confidenceInputs = candidateSignals
            .OrderBy(static signal => Array.IndexOf(SignalPrecedence, signal.SignalClass))
            .Select(static signal => new AssociationConfidenceInput(
                signal.SignalClass,
                ReasonForSignal(signal),
                NormalizeSignalWeight(signal.Weight),
                signal.EvidenceReference,
                signal.EvidenceFingerprint))
            .ToArray();
        double score = Math.Min(1.0, confidenceInputs.Sum(static confidence => confidence.Weight));
        bool requiredComplete = input.Signals.Any(static signal => signal.RequiredForAutoAssociation) &&
            candidateSignals.Any(static signal => signal.RequiredForAutoAssociation);

        return new AssociationCandidate(
            candidate.ProjectId,
            candidate.DisplayName,
            Math.Round(score, 6, MidpointRounding.AwayFromZero),
            0,
            reasons,
            evidence,
            confidenceInputs,
            requiredComplete);
    }

    private static AssociationScoringComputation FailedClosed(
        AssociationScoringInput input,
        double score,
        IReadOnlyList<AssociationReasonCode> reasons)
        => new(
            Result(
                input,
                score,
                AssociationThresholdBand.FailClosed,
                AssociationScoringOutcome.FailedClosed,
                DistinctReasons(reasons)),
            [],
            input.Exclusions);

    private static AssociationScoringResult Result(
        AssociationScoringInput input,
        double score,
        AssociationThresholdBand band,
        AssociationScoringOutcome outcome,
        IReadOnlyList<AssociationReasonCode> reasons)
        => new(
            double.IsFinite(score) ? Math.Clamp(score, 0.0, 1.0) : 0.0,
            band,
            outcome,
            reasons,
            input.KernelVersion,
            input.DetectedAt.ToUniversalTime(),
            input.SourceMailboxId,
            input.IntakeId,
            input.SourceConversationId,
            input.SourceThreadId,
            input.CorrelationId,
            MetadataOnlyRedactionState,
            CollaborationRetentionClass,
            ResultSchemaVersion);

    private static AssociationThresholdBand BandFor(double score, AssociationThresholdPolicySnapshot policy)
        => score >= policy.THigh
            ? AssociationThresholdBand.Auto
            : score >= policy.TLow
                ? AssociationThresholdBand.Ambiguous
                : AssociationThresholdBand.FailClosed;

    private static bool HasConflictingRequiredEvidence(IReadOnlyList<AssociationDeterministicSignal> signals)
        => signals
            .Where(static signal => signal.RequiredForAutoAssociation)
            .GroupBy(static signal => signal.SignalClass)
            .Any(static group => group.Select(static signal => signal.ProjectId).Distinct(StringComparer.Ordinal).Count() > 1);

    private static AssociationReasonCode ReasonForSignal(AssociationDeterministicSignal signal)
        => signal.SignalClass switch
        {
            AssociationSignalClass.ExplicitProjectIdentifier => AssociationReasonCode.ExplicitProjectIdentifierMatched,
            AssociationSignalClass.MailboxRoutingRule => AssociationReasonCode.MailboxRoutingRuleMatched,
            AssociationSignalClass.ConversationThreadIdentifier => AssociationReasonCode.ConversationThreadMatched,
            _ => AssociationReasonCode.ScorerError,
        };

    private static int ReasonPrecedence(IReadOnlyList<AssociationReasonCode> reasons)
        => reasons
            .Select(static reason => reason switch
            {
                AssociationReasonCode.ExplicitProjectIdentifierMatched => 0,
                AssociationReasonCode.MailboxRoutingRuleMatched => 1,
                AssociationReasonCode.ConversationThreadMatched => 2,
                _ => 100,
            })
            .DefaultIfEmpty(100)
            .Min();

    private static double NormalizeSignalWeight(double weight)
        => Math.Clamp(weight, 0.0, 1.0);

    private static AssociationReasonCode[] DistinctReasons(IEnumerable<AssociationReasonCode> reasons)
        => reasons
            .Distinct()
            .OrderBy(static reason => ReasonPrecedence(reason))
            .ToArray();

    private static int ReasonPrecedence(AssociationReasonCode reason)
        => reason switch
        {
            AssociationReasonCode.ExplicitProjectIdentifierMatched => 0,
            AssociationReasonCode.MailboxRoutingRuleMatched => 1,
            AssociationReasonCode.ConversationThreadMatched => 2,
            AssociationReasonCode.RequiredEvidencePresent => 3,
            AssociationReasonCode.MissingRequiredEvidence => 4,
            AssociationReasonCode.MultipleAuthorizedCandidates => 5,
            AssociationReasonCode.NoAuthorizedCandidate => 6,
            AssociationReasonCode.ConflictingDeterministicEvidence => 7,
            AssociationReasonCode.AuthorizationEvidenceUnavailable => 8,
            AssociationReasonCode.UnauthorizedCandidateSuppressed => 9,
            AssociationReasonCode.ScorerError => 10,
            _ => 100,
        };
}
