using Hexalith.ChatBot.Contracts.Commands;
using Hexalith.ChatBot.Contracts.Identities;
using Hexalith.ChatBot.Server.Association.Intake;
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

    public static DomainResult Handle(CaptureMailboxMessageIntake command, GovernedOperationState? state)
    {
        ArgumentNullException.ThrowIfNull(command);

        if (!MailboxMessageIntakeId.TryParse(command.IntakeId, out _))
        {
            return Invalid(command.IntakeId, "invalid_intake_id");
        }

        if (state is { IsMailboxIntakeCaptured: true })
        {
            return DomainResult.Rejection(new IRejectionEvent[]
            {
                new MailboxMessageIntakeAlreadyCapturedRejection(command.IntakeId),
            });
        }

        if (command.Source is null ||
            command.Recipients is null ||
            command.Attachments is null ||
            command.Source.Sender is null ||
            string.IsNullOrWhiteSpace(command.Source.ProviderMessageId) ||
            string.IsNullOrWhiteSpace(command.Source.MailboxId) ||
            string.IsNullOrWhiteSpace(command.Source.InternetMessageId) ||
            string.IsNullOrWhiteSpace(command.Source.ConversationId) ||
            string.IsNullOrWhiteSpace(command.Source.SourceContext) ||
            string.IsNullOrWhiteSpace(command.Source.Sender.Address) ||
            command.Source.SourceSchemaVersion <= 0 ||
            command.Recipients.Count == 0 ||
            command.Recipients.Any(static recipient => string.IsNullOrWhiteSpace(recipient.Address) || string.IsNullOrWhiteSpace(recipient.Kind)) ||
            command.Attachments.Any(static attachment => string.IsNullOrWhiteSpace(attachment.ProviderAttachmentId)))
        {
            return Invalid(command.IntakeId, "missing_source_identity");
        }

        return DomainResult.Success(new IEventPayload[]
        {
            new MailboxMessageIntakeCaptured(
                command.IntakeId,
                command.Source.ProviderMessageId,
                command.Source.InternetMessageId,
                command.Source.ConversationId,
                command.Source.ThreadId,
                command.Source.MailboxId,
                command.Source.Sender,
                command.Recipients,
                command.Source.ReceivedAt.ToUniversalTime(),
                command.Source.SentAt?.ToUniversalTime(),
                command.Source.CreatedAt?.ToUniversalTime(),
                command.Attachments,
                command.Source.SourceTimezone,
                command.Source.SourceContext,
                "m365-mailbox-intake",
                "mailbox-intake.kernel.v1",
                "metadata_only",
                "collaboration_input",
                command.Source.SourceSchemaVersion),
        });
    }

    private static DomainResult Invalid(string? intakeId, string reasonCode)
        => DomainResult.Rejection(new IRejectionEvent[]
        {
            new MailboxMessageIntakeInvalidRejection(intakeId, reasonCode),
        });
}
