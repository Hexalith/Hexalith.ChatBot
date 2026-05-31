using Hexalith.EventStore.Contracts.Events;

namespace Hexalith.ChatBot.Server.Operations;

/// <summary>
/// Structured rejection returned when a governed note is re-recorded against an aggregate that
/// already recorded one. Carries identifiers only (no localized text) so the idempotency cache is
/// honored and exactly one durable effect remains for a repeated submission. Returned, never thrown.
/// </summary>
/// <param name="NoteId">The ULID identifying the governed note aggregate that already exists.</param>
public sealed record GovernedNoteAlreadyRecordedRejection(string NoteId) : IRejectionEvent;
