using Hexalith.ChatBot.Contracts.Queries;
using Hexalith.EventStore.Contracts.Events;

namespace Hexalith.ChatBot.Server.Governance.AiMediation;

public sealed record ApprovedAiActionExecutionSucceeded(
    ApprovedAiActionExecutionRecord Record,
    string ProjectId,
    string RequesterId,
    string SourceMessageId,
    string? SourceConversationItemId) : IEventPayload;
