using Hexalith.ChatBot.Contracts.Commands;
using Hexalith.ChatBot.Contracts.Enums;
using Hexalith.ChatBot.Server.Association.Scoring;

using Shouldly;

namespace Hexalith.ChatBot.Server.Tests.Association.Scoring;

public sealed class DeterministicAssociationScorerTests
{
    private static readonly DateTimeOffset DetectedAt = new(2026, 5, 31, 9, 0, 0, TimeSpan.Zero);

    [Fact]
    public void ScoreShouldAutoAssociateSingleHighConfidenceAuthorizedCandidate()
    {
        AssociationScoringComputation result = DeterministicAssociationScorer.Score(Input(
            [
                Signal(AssociationSignalClass.ExplicitProjectIdentifier, "project-001", 0.7, required: true),
                Signal(AssociationSignalClass.MailboxRoutingRule, "project-001", 0.2, required: false),
            ]));

        result.Result.ConfidenceScore.ShouldBe(0.9);
        result.Result.ThresholdBand.ShouldBe(AssociationThresholdBand.Auto);
        result.Result.Outcome.ShouldBe(AssociationScoringOutcome.AutoAssociated);
        result.Candidates.ShouldHaveSingleItem().RequiredEvidenceComplete.ShouldBeTrue();
    }

    [Fact]
    public void ScoreShouldUseDeterministicOrderingAndReasonCodeDeDuplication()
    {
        AssociationScoringComputation result = DeterministicAssociationScorer.Score(Input(
            [
                Signal(AssociationSignalClass.ConversationThreadIdentifier, "project-b", 0.6, required: false),
                Signal(AssociationSignalClass.MailboxRoutingRule, "project-a", 0.6, required: false),
                Signal(AssociationSignalClass.MailboxRoutingRule, "project-a", 0.1, required: false, suffix: "dup"),
            ]));

        result.Candidates[0].ProjectId.ShouldBe("project-a");
        result.Candidates[0].ReasonCodes.Count(static reason => reason == AssociationReasonCode.MailboxRoutingRuleMatched).ShouldBe(1);
        result.Candidates[1].ProjectId.ShouldBe("project-b");
    }

    [Fact]
    public void ScoreShouldClassifyThresholdBandCandidatesForReviewRouting()
    {
        AssociationScoringComputation ambiguous = DeterministicAssociationScorer.Score(Input(
            [Signal(AssociationSignalClass.ConversationThreadIdentifier, "project-a", 0.75, required: false)]));

        ambiguous.Result.ConfidenceScore.ShouldBe(0.75);
        ambiguous.Result.ThresholdBand.ShouldBe(AssociationThresholdBand.Ambiguous);
        ambiguous.Result.Outcome.ShouldBe(AssociationScoringOutcome.CandidatesGenerated);
        ambiguous.Candidates.ShouldHaveSingleItem().ProjectId.ShouldBe("project-a");

        AssociationScoringComputation lowConfidence = DeterministicAssociationScorer.Score(Input(
            [Signal(AssociationSignalClass.ConversationThreadIdentifier, "project-b", 0.55, required: false)]));

        lowConfidence.Result.ConfidenceScore.ShouldBe(0.55);
        lowConfidence.Result.ThresholdBand.ShouldBe(AssociationThresholdBand.FailClosed);
        lowConfidence.Result.Outcome.ShouldBe(AssociationScoringOutcome.CandidatesGenerated);
        lowConfidence.Candidates.ShouldHaveSingleItem().ProjectId.ShouldBe("project-b");
    }

    [Fact]
    public void ScoreShouldKeepHighConfidenceMultipleCandidateResultsOutOfAutoAssociation()
    {
        AssociationScoringComputation result = DeterministicAssociationScorer.Score(Input(
            [
                Signal(AssociationSignalClass.ExplicitProjectIdentifier, "project-a", 0.9, required: true),
                Signal(AssociationSignalClass.ExplicitProjectIdentifier, "project-b", 0.9, required: true, suffix: "2"),
            ],
            allowConflictingRequired: true));

        result.Result.ThresholdBand.ShouldBe(AssociationThresholdBand.Auto);
        result.Result.Outcome.ShouldBe(AssociationScoringOutcome.CandidatesGenerated);
        result.Candidates.Count.ShouldBe(2);
    }

    [Fact]
    public void ScoreShouldFailClosedForConflictingRequiredEvidenceAndNonFiniteWeights()
    {
        AssociationScoringComputation conflict = DeterministicAssociationScorer.Score(Input(
            [
                Signal(AssociationSignalClass.ExplicitProjectIdentifier, "project-a", 0.9, required: true),
                Signal(AssociationSignalClass.ExplicitProjectIdentifier, "project-b", 0.9, required: true),
            ]));
        conflict.Result.Outcome.ShouldBe(AssociationScoringOutcome.FailedClosed);
        conflict.Candidates.ShouldBeEmpty();

        AssociationScoringComputation nonFinite = DeterministicAssociationScorer.Score(Input(
            [Signal(AssociationSignalClass.ExplicitProjectIdentifier, "project-a", double.NaN, required: true)]));
        nonFinite.Result.ReasonCodes.ShouldContain(AssociationReasonCode.ScorerError);
    }

    [Fact]
    public void ScoreShouldNotLeakUnauthorizedCandidateNames()
    {
        AssociationScoringComputation result = DeterministicAssociationScorer.Score(Input(
            [Signal(AssociationSignalClass.ExplicitProjectIdentifier, "project-001", 0.9, required: true)],
            exclusions:
            [
                new AssociationExclusion(
                    "project-secret",
                    AssociationExclusionState.Unauthorized,
                    AssociationReasonCode.UnauthorizedCandidateSuppressed,
                    "mailbox:secret",
                    "hash-secret"),
            ]));

        string serialized = System.Text.Json.JsonSerializer.Serialize(result);
        serialized.ShouldNotContain("Secret Project", Case.Sensitive);
        serialized.ShouldNotContain("sender@example.test", Case.Insensitive);
    }

    [Fact]
    public void ScoreShouldIgnoreNonDeterministicSignalClassesForM0Scoring()
    {
        AssociationScoringComputation result = DeterministicAssociationScorer.Score(Input(
            [
                Signal(AssociationSignalClass.ExplicitProjectIdentifier, "project-001", 0.7, required: true),
                Signal(AssociationSignalClass.Correction, "project-001", 0.5, required: true, suffix: "correction"),
            ]));

        // Only the deterministic explicit-identifier weight (0.7) counts toward the M0 score. The learned
        // correction weight (0.5) must not contribute, so it cannot tip the result over T_high (0.9) into an
        // auto-association. [AC2: M0 must not use learned/AI signals for the decision]
        result.Result.ConfidenceScore.ShouldBe(0.7);
        result.Result.ThresholdBand.ShouldBe(AssociationThresholdBand.Ambiguous);
        result.Result.Outcome.ShouldBe(AssociationScoringOutcome.CandidatesGenerated);

        AssociationCandidate candidate = result.Candidates.ShouldHaveSingleItem();
        candidate.ConfidenceScore.ShouldBe(0.7);
        candidate.ReasonCodes.ShouldNotContain(AssociationReasonCode.ScorerError);
        candidate.ConfidenceInputs.ShouldHaveSingleItem().SignalClass.ShouldBe(AssociationSignalClass.ExplicitProjectIdentifier);
        candidate.EvidenceRefs.ShouldHaveSingleItem().EvidenceKind.ShouldBe("explicit-project-identifier");

        // The non-deterministic signal leaks neither an "unknown" wire token nor its evidence fingerprint.
        string serialized = System.Text.Json.JsonSerializer.Serialize(result);
        serialized.ShouldNotContain("unknown", Case.Insensitive);
        serialized.ShouldNotContain("hash-project-001-correction", Case.Insensitive);
    }

    [Theory]
    [InlineData(MailboxAuthenticityStrictness.Permissive, AssociationScoringOutcome.AutoAssociated, null)]
    [InlineData(MailboxAuthenticityStrictness.Strict, AssociationScoringOutcome.CandidatesGenerated, AssociationReasonCode.ExternalSenderStrictReview)]
    [InlineData(MailboxAuthenticityStrictness.Paranoid, AssociationScoringOutcome.FailedClosed, AssociationReasonCode.ExternalSenderParanoidFailClosed)]
    public void ScoreShouldApplyExternalSenderStrictnessWithoutChangingWeights(
        MailboxAuthenticityStrictness strictness,
        AssociationScoringOutcome expectedOutcome,
        AssociationReasonCode? expectedReason)
    {
        AssociationScoringComputation result = DeterministicAssociationScorer.Score(Input(
            [Signal(AssociationSignalClass.ExplicitProjectIdentifier, "project-001", 0.9, required: true)],
            externalSender: ExternalSender(),
            strictnessPolicy: new MailboxAuthenticityStrictnessPolicySnapshot(strictness, "policy-v1", "configured")));

        result.Result.ConfidenceScore.ShouldBe(0.9);
        result.Result.Outcome.ShouldBe(expectedOutcome);
        result.Result.StrictnessPolicy.ShouldNotBeNull().Strictness.ShouldBe(strictness);
        result.Result.ExternalSender.ShouldNotBeNull().ExternalSender.ShouldBeTrue();
        if (expectedReason is not null)
        {
            result.Result.ReasonCodes.ShouldContain(expectedReason.Value);
        }
    }

    [Fact]
    public void ScoreShouldDefaultMissingStrictnessToStrictForExternalSender()
    {
        AssociationScoringComputation result = DeterministicAssociationScorer.Score(Input(
            [Signal(AssociationSignalClass.ExplicitProjectIdentifier, "project-001", 0.9, required: true)],
            externalSender: ExternalSender()));

        result.Result.Outcome.ShouldBe(AssociationScoringOutcome.CandidatesGenerated);
        result.Result.StrictnessPolicy.ShouldNotBeNull().Strictness.ShouldBe(MailboxAuthenticityStrictness.Strict);
        result.Result.ReasonCodes.ShouldContain(AssociationReasonCode.AuthenticityStrictnessPolicyUnavailable);
    }

    [Fact]
    public void ScoreShouldDefaultInvalidStrictnessToStrictForExternalSender()
    {
        AssociationScoringComputation result = DeterministicAssociationScorer.Score(Input(
            [Signal(AssociationSignalClass.ExplicitProjectIdentifier, "project-001", 0.9, required: true)],
            externalSender: ExternalSender(),
            strictnessPolicy: new MailboxAuthenticityStrictnessPolicySnapshot(
                (MailboxAuthenticityStrictness)99,
                "policy-v1",
                "configured")));

        result.Result.Outcome.ShouldBe(AssociationScoringOutcome.CandidatesGenerated);
        result.Result.StrictnessPolicy.ShouldNotBeNull().Strictness.ShouldBe(MailboxAuthenticityStrictness.Strict);
        result.Result.ReasonCodes.ShouldContain(AssociationReasonCode.AuthenticityStrictnessPolicyInvalid);
    }

    [Theory]
    [InlineData(MailboxAuthenticityStrictness.Permissive, AssociationScoringOutcome.AutoAssociated, null)]
    [InlineData(MailboxAuthenticityStrictness.Strict, AssociationScoringOutcome.CandidatesGenerated, AssociationReasonCode.AuthenticityStrictReview)]
    [InlineData(MailboxAuthenticityStrictness.Paranoid, AssociationScoringOutcome.FailedClosed, AssociationReasonCode.AuthenticityParanoidFailClosed)]
    public void ScoreShouldApplyStrictnessToInboundAuthenticityAnomalies(
        MailboxAuthenticityStrictness strictness,
        AssociationScoringOutcome expectedOutcome,
        AssociationReasonCode? expectedReason)
    {
        AssociationScoringComputation result = DeterministicAssociationScorer.Score(Input(
            [Signal(AssociationSignalClass.ExplicitProjectIdentifier, "project-001", 0.9, required: true)],
            strictnessPolicy: new MailboxAuthenticityStrictnessPolicySnapshot(strictness, "policy-v1", "configured"),
            authenticity: AuthenticityAnomaly()));

        result.Result.ConfidenceScore.ShouldBe(0.9);
        result.Result.Outcome.ShouldBe(expectedOutcome);
        result.Result.StrictnessPolicy.ShouldNotBeNull().Strictness.ShouldBe(strictness);
        if (expectedReason is not null)
        {
            result.Result.ReasonCodes.ShouldContain(expectedReason.Value);
        }
    }

    private static AssociationScoringInput Input(
        IReadOnlyList<AssociationDeterministicSignal> signals,
        IReadOnlyList<AssociationExclusion>? exclusions = null,
        bool allowConflictingRequired = false,
        MailboxExternalSenderPosture? externalSender = null,
        MailboxAuthenticityStrictnessPolicySnapshot? strictnessPolicy = null,
        MailboxAuthenticityMetadata? authenticity = null)
    {
        IReadOnlyList<AssociationDeterministicSignal> effectiveSignals = allowConflictingRequired
            ? signals.Select(static signal => signal with { RequiredForAutoAssociation = false }).ToArray()
            : signals;

        return new(
            "01ARZ3NDEKTSV4RRFFQ69G5FAV",
            "01ARZ3NDEKTSV4RRFFQ69G5FAY",
            "controlled-mailbox-001",
            "conversation-001",
            "thread-001",
            effectiveSignals,
            effectiveSignals
                .GroupBy(static signal => signal.ProjectId, StringComparer.Ordinal)
                .Select(static group => new ProjectAssociationCandidateEvidence(group.Key, null, group.ToArray()))
                .ToArray(),
            exclusions ?? [],
            AssociationThresholdPolicySnapshot.DefaultM0,
            DeterministicAssociationScorer.CurrentKernelVersion,
            DetectedAt,
            "01ARZ3NDEKTSV4RRFFQ69G5FAW",
            externalSender,
            strictnessPolicy,
            authenticity);
    }

    private static AssociationDeterministicSignal Signal(
        AssociationSignalClass signalClass,
        string projectId,
        double weight,
        bool required,
        string suffix = "1")
        => new(signalClass, projectId, $"mailbox:evidence:{suffix}", $"hash-{projectId}-{suffix}", weight, required);

    private static MailboxExternalSenderPosture ExternalSender()
        => new(
            ExternalSender: true,
            MailboxPartyResolutionState.Unresolved,
            ResolvedPartyRef: null,
            ["external-sender:true", "party-resolution:unresolved"]);

    private static MailboxAuthenticityMetadata AuthenticityAnomaly()
        => new(
            new MailboxAuthenticationResultSnapshot(
                MailboxAuthenticationVerdictKind.Pass,
                MailboxAuthenticationVerdictKind.Fail,
                MailboxAuthenticationVerdictKind.Pass,
                MailboxAuthenticationVerdictKind.Fail,
                "109",
                [new MailboxSelectedHeaderSnapshot("Authentication-Results", 0, MailboxHeaderValueState.Supplied)]),
            new MailboxHeaderInspectionSnapshot(
                [],
                [new MailboxSelectedHeaderSnapshot("Authentication-Results", 0, MailboxHeaderValueState.Supplied)],
                MailboxHeaderValueState.Supplied,
                MailboxHeaderValueState.NotSupplied,
                MailboxHeaderValueState.Supplied,
                MailboxHeaderValueState.NotSupplied,
                [MailboxHeaderDiscrepancyKind.FromSenderMismatch]));
}
