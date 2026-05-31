using Hexalith.EventStore.Contracts.Events;

namespace Hexalith.ChatBot.Server.Association.Intake;

/// <summary>
/// Structured rejection for invalid mailbox-intake metadata. Carries stable reason codes only.
/// </summary>
/// <param name="IntakeId">The requested mailbox intake aggregate ULID when available.</param>
/// <param name="ReasonCode">Stable, non-localized rejection reason code.</param>
public sealed record MailboxMessageIntakeInvalidRejection(string? IntakeId, string ReasonCode) : IRejectionEvent;
