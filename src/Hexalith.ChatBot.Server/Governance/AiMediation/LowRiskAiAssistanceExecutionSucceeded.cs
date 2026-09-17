using Hexalith.ChatBot.Contracts.Commands;
using Hexalith.ChatBot.Contracts.Queries;
using Hexalith.EventStore.Contracts.Events;

namespace Hexalith.ChatBot.Server.Governance.AiMediation;

public sealed record LowRiskAiAssistanceExecutionSucceeded(
    LowRiskAiAssistanceExecutionRecord Record,
    string ProjectId,
    string RequesterId,
    string SourceMessageId,
    string? SourceConversationItemId,
    IReadOnlyList<string> AuthorizedContextReferences,
    IReadOnlyList<string> ExcludedContextReasons) : IEventPayload;
