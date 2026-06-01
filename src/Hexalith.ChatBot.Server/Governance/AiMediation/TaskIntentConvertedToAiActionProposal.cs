using Hexalith.ChatBot.Contracts.Queries;
using Hexalith.EventStore.Contracts.Events;

namespace Hexalith.ChatBot.Server.Governance.AiMediation;

public sealed record TaskIntentConvertedToAiActionProposal(
    TaskIntentRecord TaskIntent,
    AiActionProposalRecord Proposal,
    string ReviewerActorId,
    DateTimeOffset DecidedAtUtc,
    string AuditOperationId) : IEventPayload;
