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

    private static ScoreMailboxMessageAssociation Command(AssociationScoringOutcome outcome)
    {
        AssociationCandidate[] candidates =
        [
            new(
                "project-001",
                "Project One",
                0.9,
                1,
                [AssociationReasonCode.ExplicitProjectIdentifierMatched],
                [new AssociationEvidenceReference("mailbox:project-id", "hash-project", "ExplicitProjectIdentifier")],
                [new AssociationConfidenceInput(AssociationSignalClass.ExplicitProjectIdentifier, AssociationReasonCode.ExplicitProjectIdentifierMatched, 0.9, "mailbox:project-id", "hash-project")],
                true),
        ];
        AssociationScoringResult result = new(
            outcome == AssociationScoringOutcome.FailedClosed ? 0.0 : 0.9,
            outcome == AssociationScoringOutcome.FailedClosed ? AssociationThresholdBand.FailClosed : AssociationThresholdBand.Auto,
            outcome,
            outcome == AssociationScoringOutcome.FailedClosed
                ? [AssociationReasonCode.NoAuthorizedCandidate]
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
}
