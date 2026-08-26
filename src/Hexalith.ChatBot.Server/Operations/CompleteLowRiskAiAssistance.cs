using Hexalith.ChatBot.Contracts.Commands;
using Hexalith.ChatBot.Contracts.Queries;

namespace Hexalith.ChatBot.Server.Operations;

/// <summary>
/// Internal executor command that records the terminal result of a provider call which was initiated from a
/// persisted <see cref="Governance.AiMediation.LowRiskAiAssistanceExecutionStarted"/> event.
/// </summary>
public sealed record CompleteLowRiskAiAssistance(
    ExecuteLowRiskAIAssistance Execution,
    string ConversationId,
    LowRiskAiAssistanceExecutionRecord Record,
    string CompletionId,
    string SchemaVersion = "chatbot.low-risk-ai-assistance-completion.v1");
