using System.Text;
using System.Text.Json;

using Hexalith.ChatBot.Contracts.Commands;
using Hexalith.ChatBot.Server.Operations;
using Hexalith.EventStore.Contracts.Commands;
using Hexalith.EventStore.Contracts.Events;
using Hexalith.EventStore.Contracts.Results;

using Shouldly;

namespace Hexalith.ChatBot.Server.Tests.Operations;

public static class GovernedOperationAggregateTests
{
    private const string NoteId = "01ARZ3NDEKTSV4RRFFQ69G5FAY";

    [Fact]
    public static void HandleOnNewAggregateShouldRecordTheNote()
    {
        DomainResult result = GovernedOperationAggregate.Handle(new RecordGovernedNote(NoteId), state: null);

        result.IsSuccess.ShouldBeTrue();
        result.Events.Count.ShouldBe(1);
        GovernedNoteRecorded recorded = result.Events[0].ShouldBeOfType<GovernedNoteRecorded>();
        recorded.NoteId.ShouldBe(NoteId);
    }

    [Fact]
    public static void HandleOnAlreadyRecordedAggregateShouldRejectWithoutThrowing()
    {
        GovernedOperationState state = new();
        state.Apply(new GovernedNoteRecorded(NoteId));

        DomainResult result = GovernedOperationAggregate.Handle(new RecordGovernedNote(NoteId), state);

        result.IsRejection.ShouldBeTrue();
        result.Events.Count.ShouldBe(1);
        GovernedNoteAlreadyRecordedRejection rejection = result.Events[0].ShouldBeOfType<GovernedNoteAlreadyRecordedRejection>();
        rejection.NoteId.ShouldBe(NoteId);
        result.Events[0].ShouldBeAssignableTo<IRejectionEvent>();
    }

    [Fact]
    public static void ApplyShouldBeIdempotentOnReplay()
    {
        GovernedOperationState state = new();
        state.IsRecorded.ShouldBeFalse();

        state.Apply(new GovernedNoteRecorded(NoteId));
        state.IsRecorded.ShouldBeTrue();
        state.NoteId.ShouldBe(NoteId);

        // A duplicate event during replay must leave state unchanged (order-tolerant, idempotent).
        state.Apply(new GovernedNoteRecorded(NoteId));
        state.IsRecorded.ShouldBeTrue();
        state.NoteId.ShouldBe(NoteId);
    }

    [Fact]
    public static async Task ProcessAsyncShouldDiscoverHandleByReflectionAndProduceTheEvent()
    {
        GovernedOperationAggregate aggregate = new();
        CommandEnvelope command = Envelope(new RecordGovernedNote(NoteId));

        DomainResult result = await aggregate.ProcessAsync(command, currentState: null);

        result.IsSuccess.ShouldBeTrue();
        result.Events[0].ShouldBeOfType<GovernedNoteRecorded>().NoteId.ShouldBe(NoteId);
    }

    private static CommandEnvelope Envelope(RecordGovernedNote command)
        => new(
            MessageId: NoteId,
            TenantId: "tenant-alpha",
            Domain: "chatbot",
            AggregateId: command.NoteId,
            CommandType: nameof(RecordGovernedNote),
            // The aggregate base deserializes the payload with default (PascalCase, case-sensitive)
            // JsonSerializer options, so serialize the same way here.
            Payload: JsonSerializer.SerializeToUtf8Bytes(command),
            CorrelationId: "01ARZ3NDEKTSV4RRFFQ69G5FAW",
            CausationId: null,
            UserId: "actor-alpha",
            Extensions: null);
}
