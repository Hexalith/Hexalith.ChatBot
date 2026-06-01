using Hexalith.ChatBot.Contracts.Queries;
using Hexalith.EventStore.Contracts.Events;

namespace Hexalith.ChatBot.Server.Governance.AiMediation;

public sealed record TaskIntentDispositionMarked(
    TaskIntentRecord TaskIntent,
    string Disposition,
    string ReviewerActorId,
    DateTimeOffset DecidedAtUtc,
    string? PredecessorTaskIntentId,
    string AuditOperationId) : IEventPayload;
