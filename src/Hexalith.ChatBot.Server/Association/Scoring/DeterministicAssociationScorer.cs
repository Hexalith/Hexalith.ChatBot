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

        // M0 scores only the deterministic signal classes (explicit project id, mailbox routing rule,
        // conversation/thread id). Learned/correction signals such as HumanSelection or Correction must never
        // contribute to the deterministic confidence score, evidence, or reason output. [AC2: M0 must not use
        // learned/AI signals for the decision]
        AssociationDeterministicSignal[] deterministicSignals = input.Signals
            .Where(static signal => IsDeterministicM0Signal(signal.SignalClass))
            .ToArray();

        if (deterministicSignals.Any(static signal => !double.IsFinite(signal.Weight) || signal.Weight < 0.0))
        {
            return FailedClosed(input, 0.0, [AssociationReasonCode.ScorerError]);
        }

        if (HasConflictingRequiredEvidence(deterministicSignals))
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
            return ApplyStrictness(new AssociationScoringComputation(noCandidates, [], input.Exclusions), input);
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

        return ApplyStrictness(new AssociationScoringComputation(
            Result(input, topScore, band, outcome, DistinctReasons(reasonCodes)),
            candidates,
            input.Exclusions), input);
    }

    private static AssociationCandidate ScoreCandidate(ProjectAssociationCandidateEvidence candidate, AssociationScoringInput input)
    {
        AssociationDeterministicSignal[] candidateSignals = candidate.Signals
            .Where(signal => string.Equals(signal.ProjectId, candidate.ProjectId, StringComparison.Ordinal))
            .Where(static signal => IsDeterministicM0Signal(signal.SignalClass))
            .ToArray();
        AssociationReasonCode[] reasons = DistinctReasons(candidateSignals.Select(ReasonForSignal));
        AssociationEvidenceReference[] evidence = candidateSignals
            .OrderBy(static signal => Array.IndexOf(SignalPrecedence, signal.SignalClass))
            .Select(static signal => new AssociationEvidenceReference(
                signal.EvidenceReference,
                signal.EvidenceFingerprint,
                SignalClassWireToken(signal.SignalClass)))
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
        bool requiredComplete = input.Signals.Any(static signal =>
                signal.RequiredForAutoAssociation && IsDeterministicM0Signal(signal.SignalClass)) &&
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
            ResultSchemaVersion,
            input.ExternalSender,
            EffectiveStrictness(input).Policy,
            null);

    private static AssociationScoringComputation ApplyStrictness(
        AssociationScoringComputation computation,
        AssociationScoringInput input)
    {
        StrictnessEvaluation strictness = EffectiveStrictness(input);
        AssociationReasonCode[] policyReasons = strictness.Reason is null ? [] : [strictness.Reason.Value];
        bool hasExternalSenderRisk = input.ExternalSender?.ExternalSender == true;
        bool hasAuthenticityRisk = HasHighRiskAuthenticityPosture(input.Authenticity);
        if (!hasExternalSenderRisk && !hasAuthenticityRisk)
        {
            return computation with
            {
                Result = computation.Result with
                {
                    StrictnessPolicy = strictness.Policy,
                    RoutingReason = strictness.Reason?.ToString(),
                    ReasonCodes = DistinctReasons([.. computation.Result.ReasonCodes, .. policyReasons]),
                },
            };
        }

        return strictness.Policy.Strictness switch
        {
            MailboxAuthenticityStrictness.Permissive => computation with
            {
                Result = computation.Result with
                {
                    StrictnessPolicy = strictness.Policy,
                    RoutingReason = "permissive",
                    ReasonCodes = DistinctReasons([.. computation.Result.ReasonCodes, .. policyReasons]),
                },
            },
            MailboxAuthenticityStrictness.Paranoid => new AssociationScoringComputation(
                computation.Result with
                {
                    ThresholdBand = AssociationThresholdBand.FailClosed,
                    Outcome = AssociationScoringOutcome.FailedClosed,
                    ReasonCodes = DistinctReasons([
                        .. computation.Result.ReasonCodes,
                        .. StrictnessRiskReasons(
                            hasExternalSenderRisk,
                            hasAuthenticityRisk,
                            AssociationReasonCode.ExternalSenderParanoidFailClosed,
                            AssociationReasonCode.AuthenticityParanoidFailClosed),
                        .. policyReasons,
                    ]),
                    StrictnessPolicy = strictness.Policy,
                    RoutingReason = RoutingReason(
                        hasExternalSenderRisk,
                        hasAuthenticityRisk,
                        "strictness-paranoid-fail-closed"),
                },
                [],
                computation.Exclusions),
            _ when computation.Result.Outcome == AssociationScoringOutcome.AutoAssociated => computation with
            {
                Result = computation.Result with
                {
                    Outcome = AssociationScoringOutcome.CandidatesGenerated,
                    ReasonCodes = DistinctReasons([
                        .. computation.Result.ReasonCodes,
                        .. StrictnessRiskReasons(
                            hasExternalSenderRisk,
                            hasAuthenticityRisk,
                            AssociationReasonCode.ExternalSenderStrictReview,
                            AssociationReasonCode.AuthenticityStrictReview),
                        .. policyReasons,
                    ]),
                    StrictnessPolicy = strictness.Policy,
                    RoutingReason = RoutingReason(
                        hasExternalSenderRisk,
                        hasAuthenticityRisk,
                        "strictness-strict-review"),
                },
            },
            _ => computation with
            {
                Result = computation.Result with
                {
                    ReasonCodes = DistinctReasons([
                        .. computation.Result.ReasonCodes,
                        .. StrictnessRiskReasons(
                            hasExternalSenderRisk,
                            hasAuthenticityRisk,
                            AssociationReasonCode.ExternalSenderStrictReview,
                            AssociationReasonCode.AuthenticityStrictReview),
                        .. policyReasons,
                    ]),
                    StrictnessPolicy = strictness.Policy,
                    RoutingReason = RoutingReason(
                        hasExternalSenderRisk,
                        hasAuthenticityRisk,
                        "strictness-strict-review"),
                },
            },
        };
    }

    private static bool HasHighRiskAuthenticityPosture(MailboxAuthenticityMetadata? authenticity)
        => authenticity is not null &&
            (authenticity.HeaderInspection?.Discrepancies is { Count: > 0 } ||
                (authenticity.AuthenticationResults is not null &&
                    (IsHighRiskVerdict(authenticity.AuthenticationResults.Spf) ||
                        IsHighRiskVerdict(authenticity.AuthenticationResults.Dkim) ||
                        IsHighRiskVerdict(authenticity.AuthenticationResults.Dmarc) ||
                        IsHighRiskVerdict(authenticity.AuthenticationResults.CompositeAuthentication))));

    private static bool IsHighRiskVerdict(MailboxAuthenticationVerdictKind verdict)
        => verdict is
            MailboxAuthenticationVerdictKind.Fail or
            MailboxAuthenticationVerdictKind.SoftFail or
            MailboxAuthenticationVerdictKind.TempError or
            MailboxAuthenticationVerdictKind.PermError or
            MailboxAuthenticationVerdictKind.Unknown or
            MailboxAuthenticationVerdictKind.Malformed or
            MailboxAuthenticationVerdictKind.Ambiguous;

    private static IEnumerable<AssociationReasonCode> StrictnessRiskReasons(
        bool hasExternalSenderRisk,
        bool hasAuthenticityRisk,
        AssociationReasonCode externalReason,
        AssociationReasonCode authenticityReason)
    {
        if (hasExternalSenderRisk)
        {
            yield return externalReason;
        }

        if (hasAuthenticityRisk)
        {
            yield return authenticityReason;
        }
    }

    private static string RoutingReason(bool hasExternalSenderRisk, bool hasAuthenticityRisk, string suffix)
        => (hasExternalSenderRisk, hasAuthenticityRisk) switch
        {
            (true, true) => $"external-sender-authenticity-{suffix}",
            (true, false) => $"external-sender-{suffix}",
            (false, true) => $"authenticity-{suffix}",
            _ => suffix,
        };

    private static StrictnessEvaluation EffectiveStrictness(AssociationScoringInput input)
    {
        if (input.StrictnessPolicy is null)
        {
            return new StrictnessEvaluation(
                new MailboxAuthenticityStrictnessPolicySnapshot(
                    MailboxAuthenticityStrictness.Strict,
                    "policy-unavailable",
                    "policy-unavailable"),
                AssociationReasonCode.AuthenticityStrictnessPolicyUnavailable);
        }

        if (!Enum.IsDefined(input.StrictnessPolicy.Strictness))
        {
            return new StrictnessEvaluation(
                input.StrictnessPolicy with
                {
                    Strictness = MailboxAuthenticityStrictness.Strict,
                    ReasonCode = "policy-invalid",
                },
                AssociationReasonCode.AuthenticityStrictnessPolicyInvalid);
        }

        return new StrictnessEvaluation(input.StrictnessPolicy, null);
    }

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

    private static bool IsDeterministicM0Signal(AssociationSignalClass signalClass)
        => Array.IndexOf(SignalPrecedence, signalClass) >= 0;

    private static AssociationReasonCode ReasonForSignal(AssociationDeterministicSignal signal)
        => signal.SignalClass switch
        {
            AssociationSignalClass.ExplicitProjectIdentifier => AssociationReasonCode.ExplicitProjectIdentifierMatched,
            AssociationSignalClass.MailboxRoutingRule => AssociationReasonCode.MailboxRoutingRuleMatched,
            AssociationSignalClass.ConversationThreadIdentifier => AssociationReasonCode.ConversationThreadMatched,
            _ => AssociationReasonCode.ScorerError,
        };

    private static string SignalClassWireToken(AssociationSignalClass signalClass)
        => signalClass switch
        {
            AssociationSignalClass.ExplicitProjectIdentifier => "explicit-project-identifier",
            AssociationSignalClass.MailboxRoutingRule => "mailbox-routing-rule",
            AssociationSignalClass.ConversationThreadIdentifier => "conversation-thread-identifier",
            _ => "unknown",
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
            AssociationReasonCode.ExternalSenderStrictReview => 11,
            AssociationReasonCode.ExternalSenderParanoidFailClosed => 12,
            AssociationReasonCode.AuthenticityStrictReview => 13,
            AssociationReasonCode.AuthenticityParanoidFailClosed => 14,
            AssociationReasonCode.AuthenticityStrictnessPolicyUnavailable => 15,
            AssociationReasonCode.AuthenticityStrictnessPolicyInvalid => 16,
            _ => 100,
        };

    private sealed record StrictnessEvaluation(
        MailboxAuthenticityStrictnessPolicySnapshot Policy,
        AssociationReasonCode? Reason);
}
