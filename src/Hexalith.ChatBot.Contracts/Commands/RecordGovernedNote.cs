namespace Hexalith.ChatBot.Contracts.Commands;

/// <summary>
/// Trivial, self-contained, in-tenant, append-only governed write used by the Story 1.9 walking
/// skeleton to exercise the full command spine end-to-end. It records that a governed note
/// identified by <see cref="NoteId"/> occurred. It intentionally carries no free-form content so
/// the spine stays metadata-only, and has no dependency on Epic 2-4 project/conversation context.
/// </summary>
/// <param name="NoteId">The ULID identifying the governed note aggregate this command targets.</param>
public sealed record RecordGovernedNote(string NoteId) : IChatBotCommand;
