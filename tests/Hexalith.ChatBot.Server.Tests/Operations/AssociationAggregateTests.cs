using Hexalith.ChatBot.Contracts.Commands;
using Hexalith.ChatBot.Contracts.Enums;
using Hexalith.ChatBot.Server.Association;
using Hexalith.ChatBot.Server.Association.Scoring;
using Hexalith.ChatBot.Server.Operations;
using Hexalith.EventStore.Contracts.Commands;
using Hexalith.EventStore.Contracts.Events;
using Hexalith.EventStore.Contracts.Results;

using Shouldly;

namespace Hexalith.ChatBot.Server.Tests.Operations;

public static class AssociationAggregateTests
{
    private const string AssociationId = "01ARZ3NDEKTSV4RRFFQ69G5FAV";
    private const string IntakeId = "01ARZ3NDEKTSV4RRFFQ69G5FAY";
    private const string CorrelationId = "01ARZ3NDEKTSV4RRFFQ69G5FAW";
    private const string Tenant = "tenant-alpha";

    [Fact]
    public static void HandleAssociationScoringShouldEmitAutoAssociatedMetadataOnlyEvent()
    {
        DomainResult result = GovernedOperationAggregate.Handle(Command(AssociationScoringOutcome.AutoAssociated), null, Envelope());

        result.IsSuccess.ShouldBeTrue();
        MailboxEmailAssociatedToProject associated = result.Events.ShouldHaveSingleItem().ShouldBeOfType<MailboxEmailAssociatedToProject>();
        associated.TenantId.ShouldBe(Tenant);
        associated.IntakeId.ShouldBe(IntakeId);
        associated.ProjectId.ShouldBe("project-001");
        associated.ConfidenceScore.ShouldBe(0.9);
        associated.CorrelationId.ShouldBe(CorrelationId);
        associated.RedactionState.ShouldBe("metadata_only");
        associated.ActorId.ShouldBe("actor-alpha");
        associated.ActorType.ShouldBe("system");
        associated.DecisionKind.ShouldBe("associate");
        associated.SurfaceOrigin.ShouldBe("worker");
        associated.DecidedAt.ShouldBe(new DateTimeOffset(2026, 5, 31, 9, 0, 0, TimeSpan.Zero));

        string serialized = System.Text.Json.JsonSerializer.Serialize(associated);
        serialized.ShouldNotContain("sender@example.test", Case.Insensitive);
    }

    [Fact]
    public static void HandleAssociationScoringShouldEmitFailedClosedForNonAutoOutcome()
    {
        DomainResult result = GovernedOperationAggregate.Handle(
            Command(AssociationScoringOutcome.FailedClosed) with { Candidates = [] },
            null,
            Envelope());

        result.Events.ShouldHaveSingleItem().ShouldBeOfType<MailboxAssociationScoringFailedClosed>()
            .ReasonCodes.ShouldContain(AssociationReasonCode.NoAuthorizedCandidate);
    }

    [Fact]
    public static void HandleAssociationScoringShouldRouteAmbiguousCandidatesToNeedsReview()
    {
        ScoreMailboxMessageAssociation command = Command(
            AssociationScoringOutcome.CandidatesGenerated,
            AssociationThresholdBand.Ambiguous,
            0.75);

        DomainResult result = GovernedOperationAggregate.Handle(command, null, Envelope());

        result.IsSuccess.ShouldBeTrue();
        result.Events.ShouldNotContain(static e => e is MailboxEmailAssociatedToProject);
        MailboxAssociationCandidatesGenerated routed = result.Events.ShouldHaveSingleItem().ShouldBeOfType<MailboxAssociationCandidatesGenerated>();
        routed.LifecycleState.ShouldBe(LifecycleState.NeedsReview);
        routed.Candidates.ShouldHaveSingleItem().ProjectId.ShouldBe("project-001");
        routed.ThresholdBand.ShouldBe(AssociationThresholdBand.Ambiguous);
        routed.Outcome.ShouldBe(AssociationScoringOutcome.CandidatesGenerated);
    }

    [Fact]
    public static void HandleAssociationScoringShouldRouteLowConfidenceCandidatesToNeedsReviewAndPreserveCandidates()
    {
        ScoreMailboxMessageAssociation command = Command(
            AssociationScoringOutcome.CandidatesGenerated,
            AssociationThresholdBand.FailClosed,
            0.55);

        DomainResult result = GovernedOperationAggregate.Handle(command, null, Envelope());

        result.IsSuccess.ShouldBeTrue();
        MailboxAssociationCandidatesGenerated routed = result.Events.ShouldHaveSingleItem().ShouldBeOfType<MailboxAssociationCandidatesGenerated>();
        routed.LifecycleState.ShouldBe(LifecycleState.NeedsReview);
        routed.Candidates.ShouldHaveSingleItem().ConfidenceScore.ShouldBe(0.55);
        routed.ReasonCodes.ShouldContain(AssociationReasonCode.MissingRequiredEvidence);
    }

    [Fact]
    public static void HandleAssociationDecisionShouldEmitActorAttributedMetadataOnlyEvent()
    {
        GovernedOperationState state = StateWithNeedsReviewCandidates();
        AssociateEmailToProject command = new(
            AssociationId,
            IntakeId,
            "project-001",
            AssociationDecisionKind.Associate,
            "Reviewed safe metadata.",
            "hash-project",
            1,
            "chatbot.association-decision-command.v1");

        DomainResult result = GovernedOperationAggregate.Handle(command, state, DecisionEnvelope(nameof(AssociateEmailToProject)));

        result.IsSuccess.ShouldBeTrue();
        MailboxEmailAssociationConfirmed confirmed = result.Events.ShouldHaveSingleItem().ShouldBeOfType<MailboxEmailAssociationConfirmed>();
        confirmed.TenantId.ShouldBe(Tenant);
        confirmed.ActorId.ShouldBe("actor-alpha");
        confirmed.ActorType.ShouldBe("human");
        confirmed.SurfaceOrigin.ShouldBe("ui");
        confirmed.DecisionKind.ShouldBe(AssociationDecisionKind.Associate);
        confirmed.ProjectId.ShouldBe("project-001");
        confirmed.EvidenceRefs.ShouldHaveSingleItem().EvidenceFingerprint.ShouldBe("hash-project");
        confirmed.DecisionNote.ShouldBe("Reviewed safe metadata.");
        confirmed.RedactionState.ShouldBe("metadata_only");
        confirmed.SourceVersion.ShouldBe(2);

        string serialized = System.Text.Json.JsonSerializer.Serialize(confirmed);
        serialized.ShouldNotContain("sender@example.test", Case.Insensitive);
        serialized.ShouldNotContain("raw-body", Case.Insensitive);
    }

    [Fact]
    public static void HandleAssociationDecisionShouldRejectStaleEvidenceAndUnsafeNotes()
    {
        GovernedOperationState state = StateWithNeedsReviewCandidates();
        AssociateEmailToProject stale = new(
            AssociationId,
            IntakeId,
            "project-001",
            AssociationDecisionKind.Associate,
            null,
            "old-hash",
            1,
            "chatbot.association-decision-command.v1");
        RejectEmailProjectAssociation unsafeNote = new(
            AssociationId,
            IntakeId,
            AssociationDecisionKind.Reject,
            "Bearer secret raw-body",
            "hash-project",
            1,
            "chatbot.association-decision-command.v1");

        GovernedOperationAggregate.Handle(stale, state, DecisionEnvelope(nameof(AssociateEmailToProject)))
            .Events[0].ShouldBeOfType<MailboxAssociationDecisionInvalidRejection>()
            .ReasonCode.ShouldBe("stale_evidence");
        GovernedOperationAggregate.Handle(unsafeNote, state, DecisionEnvelope(nameof(RejectEmailProjectAssociation)))
            .Events[0].ShouldBeOfType<MailboxAssociationDecisionInvalidRejection>()
            .ReasonCode.ShouldBe("invalid_decision_note");
    }

    [Fact]
    public static void HandleAssociationCorrectionShouldSupersedePriorDecisionWithoutMutatingHistory()
    {
        GovernedOperationState state = StateWithAssociatedDecision();
        CorrectEmailProjectAssociation command = new(
            AssociationId,
            IntakeId,
            "project-001",
            "project-002",
            AssociationCorrectionKind.ProjectReassignment,
            "Wrong project selected from safe metadata.",
            AssociationId,
            "hash-project-002",
            2,
            "chatbot.association-correction-command.v1");

        DomainResult result = GovernedOperationAggregate.Handle(command, state, DecisionEnvelope(nameof(CorrectEmailProjectAssociation)));

        result.IsSuccess.ShouldBeTrue();
        MailboxEmailAssociationCorrected corrected = result.Events.ShouldHaveSingleItem().ShouldBeOfType<MailboxEmailAssociationCorrected>();
        corrected.TenantId.ShouldBe(Tenant);
        corrected.ActorId.ShouldBe("actor-alpha");
        corrected.ActorType.ShouldBe("human");
        corrected.SurfaceOrigin.ShouldBe("ui");
        corrected.CorrectionKind.ShouldBe(AssociationCorrectionKind.ProjectReassignment);
        corrected.PriorProjectId.ShouldBe("project-001");
        corrected.CorrectedProjectId.ShouldBe("project-002");
        corrected.PredecessorAssociationId.ShouldBe(AssociationId);
        corrected.SupersedesAssociationId.ShouldBe(AssociationId);
        corrected.CorrectionRationale.ShouldBe("Wrong project selected from safe metadata.");
        corrected.RedactionState.ShouldBe("metadata_only");
        corrected.SourceVersion.ShouldBe(3);

        string serialized = System.Text.Json.JsonSerializer.Serialize(corrected);
        serialized.ShouldNotContain("sender@example.test", Case.Insensitive);
        serialized.ShouldNotContain("raw-body", Case.Insensitive);
    }

    [Fact]
    public static void HandleAssociationCorrectionShouldRejectStaleUnsafeAndInvalidLifecycleRequests()
    {
        GovernedOperationState associated = StateWithAssociatedDecision();
        GovernedOperationState needsReview = StateWithNeedsReviewCandidates();

        CorrectEmailProjectAssociation stale = CorrectionCommand(sourceVersion: 1);
        CorrectEmailProjectAssociation unsafeRationale = CorrectionCommand(rationale: "Bearer secret raw-body");
        CorrectEmailProjectAssociation staleFingerprint = CorrectionCommand(evidenceFingerprint: "untrusted-fingerprint");
        CorrectEmailProjectAssociation invalidLifecycle = CorrectionCommand();

        GovernedOperationAggregate.Handle(stale, associated, DecisionEnvelope(nameof(CorrectEmailProjectAssociation)))
            .Events[0].ShouldBeOfType<MailboxAssociationCorrectionInvalidRejection>()
            .ReasonCode.ShouldBe("stale_evidence");
        GovernedOperationAggregate.Handle(unsafeRationale, associated, DecisionEnvelope(nameof(CorrectEmailProjectAssociation)))
            .Events[0].ShouldBeOfType<MailboxAssociationCorrectionInvalidRejection>()
            .ReasonCode.ShouldBe("invalid_correction_rationale");
        GovernedOperationAggregate.Handle(staleFingerprint, associated, DecisionEnvelope(nameof(CorrectEmailProjectAssociation)))
            .Events[0].ShouldBeOfType<MailboxAssociationCorrectionInvalidRejection>()
            .ReasonCode.ShouldBe("stale_evidence");
        GovernedOperationAggregate.Handle(invalidLifecycle, needsReview, DecisionEnvelope(nameof(CorrectEmailProjectAssociation)))
            .Events[0].ShouldBeOfType<MailboxAssociationCorrectionInvalidRejection>()
            .ReasonCode.ShouldBe("invalid_association_lifecycle_transition");
    }

    [Fact]
    public static void HandleAssociationScoringShouldRouteScorerErrorFailClosedToNeedsReviewWithEmptyCandidates()
    {
        ScoreMailboxMessageAssociation command = Command(
            AssociationScoringOutcome.FailedClosed,
            AssociationThresholdBand.FailClosed,
            0.0) with
        {
            Candidates = [],
            Result = Command(AssociationScoringOutcome.FailedClosed).Result! with
            {
                ReasonCodes = [AssociationReasonCode.ScorerError],
            },
        };

        DomainResult result = GovernedOperationAggregate.Handle(command, null, Envelope());

        result.IsSuccess.ShouldBeTrue();
        MailboxAssociationScoringFailedClosed routed = result.Events.ShouldHaveSingleItem().ShouldBeOfType<MailboxAssociationScoringFailedClosed>();
        routed.LifecycleState.ShouldBe(LifecycleState.NeedsReview);
        routed.ReasonCodes.ShouldContain(AssociationReasonCode.ScorerError);
    }

    [Fact]
    public static void HandleAssociationScoringShouldRejectAutoAssociationBelowPolicyThreshold()
    {
        ScoreMailboxMessageAssociation command = Command(AssociationScoringOutcome.AutoAssociated) with
        {
            Candidates =
            [
                Command(AssociationScoringOutcome.AutoAssociated).Candidates![0] with { ConfidenceScore = 0.79 },
            ],
            Result = Command(AssociationScoringOutcome.AutoAssociated).Result! with
            {
                ConfidenceScore = 0.79,
            },
        };

        DomainResult result = GovernedOperationAggregate.Handle(command, null, Envelope());

        result.IsRejection.ShouldBeTrue();
        result.Events[0].ShouldBeOfType<MailboxAssociationInvalidRejection>()
            .ReasonCode.ShouldBe("invalid_auto_association_scoring_payload");
    }

    [Fact]
    public static void HandleAssociationScoringShouldRejectFailedClosedPayloadWithCandidates()
    {
        DomainResult result = GovernedOperationAggregate.Handle(Command(AssociationScoringOutcome.FailedClosed), null, Envelope());

        result.IsRejection.ShouldBeTrue();
        result.Events[0].ShouldBeOfType<MailboxAssociationInvalidRejection>()
            .ReasonCode.ShouldBe("invalid_fail_closed_association_scoring_payload");
    }

    [Fact]
    public static void HandleInvalidAssociationScoringShouldReturnStructuredRejection()
    {
        DomainResult result = GovernedOperationAggregate.Handle(Command(AssociationScoringOutcome.AutoAssociated) with { AssociationId = "not-a-ulid" }, null, Envelope());

        result.IsRejection.ShouldBeTrue();
        result.Events[0].ShouldBeOfType<MailboxAssociationInvalidRejection>().ReasonCode.ShouldBe("invalid_association_identity");
        result.Events[0].ShouldBeAssignableTo<IRejectionEvent>();
    }

    [Fact]
    public static void HandleThresholdPolicyShouldRejectUnsafeM0FloorsWithoutEvaluationRun()
    {
        SetAssociationConfidenceThresholds command = new("association", 0.79, 0.55, "policy-v1", null, new DateTimeOffset(2026, 5, 31, 9, 0, 0, TimeSpan.Zero));

        DomainResult result = GovernedOperationAggregate.Handle(command, null, Envelope());

        result.IsRejection.ShouldBeTrue();
        result.Events[0].ShouldBeOfType<AssociationThresholdPolicyInvalidRejection>().ReasonCode.ShouldBe("invalid_threshold_policy");
    }

    [Fact]
    public static void HandleThresholdPolicyShouldAuditPreviousAndNewThresholdValues()
    {
        SetAssociationConfidenceThresholds command = new("association", 0.91, 0.61, "policy-v1", null, new DateTimeOffset(2026, 5, 31, 9, 0, 0, TimeSpan.Zero));

        DomainResult result = GovernedOperationAggregate.Handle(command, null, Envelope());

        result.IsSuccess.ShouldBeTrue();
        AssociationConfidenceThresholdsChanged changed = result.Events.ShouldHaveSingleItem().ShouldBeOfType<AssociationConfidenceThresholdsChanged>();
        changed.PreviousTHigh.ShouldBe(AssociationThresholdPolicySnapshot.DefaultM0High);
        changed.PreviousTLow.ShouldBe(AssociationThresholdPolicySnapshot.DefaultM0Low);
        changed.PreviousPolicyVersion.ShouldBe(AssociationThresholdPolicySnapshot.DefaultM0.PolicyVersion);
        changed.THigh.ShouldBe(0.91);
        changed.TLow.ShouldBe(0.61);
    }

    private static ScoreMailboxMessageAssociation Command(
        AssociationScoringOutcome outcome,
        AssociationThresholdBand? thresholdBand = null,
        double? confidenceScore = null,
        bool includeCorrectionCandidate = false)
    {
        double score = confidenceScore ?? (outcome == AssociationScoringOutcome.FailedClosed ? 0.0 : 0.9);
        AssociationThresholdBand band = thresholdBand ?? (outcome == AssociationScoringOutcome.FailedClosed ? AssociationThresholdBand.FailClosed : AssociationThresholdBand.Auto);
        List<AssociationCandidate> candidates =
        [
            new(
                "project-001",
                "Project One",
                score,
                1,
                [AssociationReasonCode.ExplicitProjectIdentifierMatched],
                [new AssociationEvidenceReference("mailbox:project-id", "hash-project", "ExplicitProjectIdentifier")],
                [new AssociationConfidenceInput(AssociationSignalClass.ExplicitProjectIdentifier, AssociationReasonCode.ExplicitProjectIdentifierMatched, 0.9, "mailbox:project-id", "hash-project")],
                true),
        ];
        if (includeCorrectionCandidate)
        {
            candidates.Add(new AssociationCandidate(
                "project-002",
                "Project Two",
                0.74,
                2,
                [AssociationReasonCode.MailboxRoutingRuleMatched],
                [new AssociationEvidenceReference("mailbox:project-alias", "hash-project-002", "ProjectAlias")],
                [new AssociationConfidenceInput(AssociationSignalClass.MailboxRoutingRule, AssociationReasonCode.MailboxRoutingRuleMatched, 0.74, "mailbox:project-alias", "hash-project-002")],
                true));
        }
        AssociationScoringResult result = new(
            score,
            band,
            outcome,
            outcome == AssociationScoringOutcome.FailedClosed
                ? [AssociationReasonCode.NoAuthorizedCandidate]
                : band == AssociationThresholdBand.FailClosed
                    ? [AssociationReasonCode.MissingRequiredEvidence]
                    : [AssociationReasonCode.ExplicitProjectIdentifierMatched, AssociationReasonCode.RequiredEvidencePresent],
            DeterministicAssociationScorer.CurrentKernelVersion,
            new DateTimeOffset(2026, 5, 31, 9, 0, 0, TimeSpan.Zero),
            "controlled-mailbox-001",
            IntakeId,
            "conversation-001",
            "thread-001",
            CorrelationId,
            "metadata_only",
            "collaboration_input",
            DeterministicAssociationScorer.ResultSchemaVersion);

        return new ScoreMailboxMessageAssociation(
            AssociationId,
            IntakeId,
            "controlled-mailbox-001",
            "conversation-001",
            "thread-001",
            [new AssociationDeterministicSignal(AssociationSignalClass.ExplicitProjectIdentifier, "project-001", "mailbox:project-id", "hash-project", 0.9, true)],
            AssociationThresholdPolicySnapshot.DefaultM0,
            candidates,
            [],
            result,
            DeterministicAssociationScorer.CurrentKernelVersion);
    }

    private static CommandEnvelope Envelope()
        => new(
            "01ARZ3NDEKTSV4RRFFQ69G5FAA",
            Tenant,
            ChatBotEventStore.DomainName,
            AssociationId,
            nameof(ScoreMailboxMessageAssociation),
            [],
            CorrelationId,
            null,
            "actor-alpha",
            null);

    private static CommandEnvelope DecisionEnvelope(string commandType)
        => Envelope() with
        {
            CommandType = commandType,
            UserId = "actor-alpha",
            Extensions = new Dictionary<string, string>
            {
                ["surfaceOrigin"] = "ui",
                ["actorType"] = "human",
                ["decidedAt"] = "2026-05-31T09:15:00.0000000+00:00",
            },
        };

    private static GovernedOperationState StateWithNeedsReviewCandidates(bool includeCorrectionCandidate = false)
    {
        DomainResult routed = GovernedOperationAggregate.Handle(
            Command(AssociationScoringOutcome.CandidatesGenerated, AssociationThresholdBand.Ambiguous, 0.75, includeCorrectionCandidate),
            null,
            Envelope());
        GovernedOperationState state = new();
        state.Apply(routed.Events.ShouldHaveSingleItem().ShouldBeOfType<MailboxAssociationCandidatesGenerated>());
        return state;
    }

    private static GovernedOperationState StateWithAssociatedDecision()
    {
        GovernedOperationState state = StateWithNeedsReviewCandidates(includeCorrectionCandidate: true);
        AssociateEmailToProject decision = new(
            AssociationId,
            IntakeId,
            "project-001",
            AssociationDecisionKind.Associate,
            "Reviewed safe metadata.",
            "hash-project",
            1,
            "chatbot.association-decision-command.v1");
        MailboxEmailAssociationConfirmed confirmed = GovernedOperationAggregate
            .Handle(decision, state, DecisionEnvelope(nameof(AssociateEmailToProject)))
            .Events
            .ShouldHaveSingleItem()
            .ShouldBeOfType<MailboxEmailAssociationConfirmed>();
        state.Apply(confirmed);
        return state;
    }

    private static CorrectEmailProjectAssociation CorrectionCommand(
        long sourceVersion = 2,
        string? rationale = "Wrong project selected from safe metadata.",
        string evidenceFingerprint = "hash-project-002")
        => new(
            AssociationId,
            IntakeId,
            "project-001",
            "project-002",
            AssociationCorrectionKind.ProjectReassignment,
            rationale,
            AssociationId,
            evidenceFingerprint,
            sourceVersion,
            "chatbot.association-correction-command.v1");
}
