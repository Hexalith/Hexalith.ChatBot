using Hexalith.ChatBot.Contracts.Queries;
using Hexalith.ChatBot.Server.Governance.Conversations;

namespace Hexalith.ChatBot.Server.Projections;

internal sealed record PublishedTaskIntentEvent(
    string? TenantId,
    string? Domain,
    string? AggregateId,
    string? EventTypeName,
    long SequenceNumber,
    DateTimeOffset Timestamp,
    string? CorrelationId,
    TaskIntentRecord? Record,
    AiActionProposalRecord? Proposal = null,
    ProjectConversationMessageAppended? UserMessage = null,
    AiResponseGenerationCancellationRequested? AiResponseCancellation = null);
