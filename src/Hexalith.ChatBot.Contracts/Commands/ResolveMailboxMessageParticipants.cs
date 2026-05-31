namespace Hexalith.ChatBot.Contracts.Commands;

/// <summary>
/// Resolves mailbox sender and recipient source evidence to tenant-scoped PartyId references.
/// </summary>
public sealed record ResolveMailboxMessageParticipants(
    string ResolutionId,
    string IntakeId,
    string SourceMailboxId,
    IReadOnlyList<MailboxParticipantSourceReference> SourceParticipants,
    IReadOnlyList<ResolvedMailboxParticipantReference> ResolvedParticipants,
    IReadOnlyList<UnresolvedMailboxParticipantEvidence> UnresolvedParticipants,
    string ResolutionKernelVersion) : IChatBotCommand;
