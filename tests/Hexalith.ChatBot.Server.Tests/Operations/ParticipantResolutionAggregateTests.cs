using Hexalith.ChatBot.Contracts.Commands;
using Hexalith.ChatBot.Contracts.Enums;
using Hexalith.ChatBot.Server.Association.Participants;
using Hexalith.ChatBot.Server.Operations;
using Hexalith.EventStore.Contracts.Events;
using Hexalith.EventStore.Contracts.Results;

using Shouldly;

namespace Hexalith.ChatBot.Server.Tests.Operations;

public static class ParticipantResolutionAggregateTests
{
    private const string ResolutionId = "01ARZ3NDEKTSV4RRFFQ69G5FAV";
    private const string IntakeId = "01ARZ3NDEKTSV4RRFFQ69G5FAY";
    private const string SourceParticipantId = "01ARZ3NDEKTSV4RRFFQ69G5FAZ";

    [Fact]
    public static void HandleParticipantResolutionShouldEmitMetadataOnlyResolvedAndUnresolvedEvents()
    {
        DomainResult result = GovernedOperationAggregate.Handle(Command(), state: null);

        result.IsSuccess.ShouldBeTrue();
        result.Events.Count.ShouldBe(2);
        MailboxParticipantResolved resolved = result.Events[0].ShouldBeOfType<MailboxParticipantResolved>();
        resolved.ResolutionId.ShouldBe(ResolutionId);
        resolved.IntakeId.ShouldBe(IntakeId);
        resolved.SourceParticipantId.ShouldBe(SourceParticipantId);
        resolved.PartyId.ShouldBe("tenant-alpha:parties:party-001");
        resolved.PartyTenantId.ShouldBe("tenant-alpha");
        resolved.EvidenceReference.ShouldBe("mailbox:intake:sender");
        resolved.RedactionState.ShouldBe("metadata_only");

        MailboxParticipantUnresolved unresolved = result.Events[1].ShouldBeOfType<MailboxParticipantUnresolved>();
        unresolved.Reason.ShouldBe(ParticipantResolutionBlockedReason.NotFound);
        unresolved.AllowedReviewActions.ShouldContain(ParticipantReviewAction.CreatePending);

        string serialized = System.Text.Json.JsonSerializer.Serialize(result.Events);
        serialized.ShouldNotContain("sender@example.test", Case.Insensitive);
        serialized.ShouldNotContain("Sender", Case.Sensitive);
    }

    [Fact]
    public static void HandleInvalidParticipantResolutionShouldReturnStructuredRejection()
    {
        DomainResult result = GovernedOperationAggregate.Handle(Command() with { ResolutionId = "not-a-ulid" }, state: null);

        result.IsRejection.ShouldBeTrue();
        result.Events[0].ShouldBeOfType<MailboxParticipantResolutionInvalidRejection>().ReasonCode.ShouldBe("invalid_resolution_identity");
        result.Events[0].ShouldBeAssignableTo<IRejectionEvent>();
    }

    [Fact]
    public static void ApplyShouldTrackParticipantResolutionForReplayIdempotency()
    {
        GovernedOperationState state = new();
        state.Apply(new MailboxParticipantResolved(
            ResolutionId,
            IntakeId,
            SourceParticipantId,
            "tenant-alpha:parties:party-001",
            "tenant-alpha",
            "mailbox:intake:sender",
            "evidence-sha256",
            "controlled-mailbox-001",
            "m365-mailbox-intake",
            "participant-resolution.kernel.v1",
            "metadata_only",
            "collaboration_input",
            1,
            "chatbot.participant-resolution-event.v1"));

        state.ParticipantResolutionIds.ShouldContain(ResolutionId);
        GovernedOperationAggregate.Handle(Command(), state).Events[0]
            .ShouldBeOfType<MailboxParticipantResolutionInvalidRejection>()
            .ReasonCode.ShouldBe("participant_resolution_already_recorded");
    }

    private static ResolveMailboxMessageParticipants Command()
        => new(
            ResolutionId,
            IntakeId,
            "controlled-mailbox-001",
            [new MailboxParticipantSourceReference(SourceParticipantId, "sender", "mailbox:intake:sender", "evidence-sha256", "sender@example.test", "Sender")],
            [new ResolvedMailboxParticipantReference(SourceParticipantId, "tenant-alpha:parties:party-001", "tenant-alpha", "mailbox:intake:sender", "evidence-sha256", ParticipantResolutionStatus.Resolved)],
            [new UnresolvedMailboxParticipantEvidence(SourceParticipantId, "mailbox:intake:recipient:0", "recipient-evidence-sha256", ParticipantResolutionBlockedReason.NotFound, [ParticipantReviewAction.Link, ParticipantReviewAction.CreatePending, ParticipantReviewAction.Reject, ParticipantReviewAction.Quarantine])],
            "participant-resolution.kernel.v1");
}
