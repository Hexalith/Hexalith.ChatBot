using Hexalith.ChatBot.Contracts.Commands;
using Hexalith.EventStore.Client.Aggregates;
using Hexalith.EventStore.Contracts.Events;
using Hexalith.EventStore.Contracts.Results;

namespace Hexalith.ChatBot.Server.Operations;

/// <summary>
/// Event-sourced aggregate (Pattern A) for the Story 1.9 walking-skeleton governed note. The base
/// <see cref="EventStoreAggregate{TState}"/> is itself the <c>IDomainProcessor</c>: it reflection-discovers
/// the typed <see cref="Handle(RecordGovernedNote, GovernedOperationState?)"/> and the state's <c>Apply</c>
/// method. <see cref="Handle"/> is pure — no I/O, DAPR, authorization, or sibling calls — and never throws for
/// a business-rule violation (it returns a structured rejection so the idempotency cache is honored).
/// </summary>
public sealed class GovernedOperationAggregate : EventStoreAggregate<GovernedOperationState>
{
    /// <summary>
    /// Records a governed note. Fine-grained (aggregate-altitude) idempotency: recording a second note
    /// against an already-recorded aggregate yields a structured rejection rather than a duplicate event,
    /// so a repeated submission resolves to exactly one durable effect.
    /// </summary>
    /// <param name="command">The governed note command.</param>
    /// <param name="state">The replayed aggregate state, or <see langword="null"/> for a new aggregate.</param>
    /// <returns>A success result carrying the recorded-note event, or a structured rejection.</returns>
    public static DomainResult Handle(RecordGovernedNote command, GovernedOperationState? state)
    {
        ArgumentNullException.ThrowIfNull(command);

        if (state is { IsRecorded: true })
        {
            return DomainResult.Rejection(new IRejectionEvent[]
            {
                new GovernedNoteAlreadyRecordedRejection(command.NoteId),
            });
        }

        return DomainResult.Success(new IEventPayload[]
        {
            new GovernedNoteRecorded(command.NoteId),
        });
    }
}
