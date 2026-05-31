using Hexalith.EventStore.Contracts.Events;

namespace Hexalith.ChatBot.Server.Association.Intake;

/// <summary>
/// Structured aggregate-altitude rejection for replaying the same ChatBot-owned intake aggregate id.
/// </summary>
/// <param name="IntakeId">The mailbox intake aggregate ULID.</param>
public sealed record MailboxMessageIntakeAlreadyCapturedRejection(string IntakeId) : IRejectionEvent;
