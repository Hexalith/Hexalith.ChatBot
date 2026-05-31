using Hexalith.EventStore.Contracts.Events;

namespace Hexalith.ChatBot.Server.Operations;

/// <summary>
/// Past-tense domain event recording that a governed note aggregate was created. Payload-only and
/// structured (an identifier only) — EventStore owns correlation/causation/tenant/persistence/publish
/// metadata, so no envelope fields, timestamps, or display text appear here (metadata-only invariant).
/// </summary>
/// <param name="NoteId">The ULID identifying the governed note aggregate.</param>
public sealed record GovernedNoteRecorded(string NoteId) : IEventPayload;
