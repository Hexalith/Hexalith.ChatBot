using Hexalith.ChatBot.Contracts.Queries;
using Hexalith.EventStore.Contracts.Events;

namespace Hexalith.ChatBot.Server.Governance.AiMediation;

public sealed record LowRiskAiAssistanceExecutionStarted(
    string ExecutionId,
    string ProposalId,
    string ProjectId,
    string TaskIntentId,
    string SourceMessageId,
    string RequesterId,
    string AssistanceKind,
    string ContextPackageId,
    string ContextPackageVersion,
    string PolicySnapshotId,
    string PolicyReasonCode,
    long ExpectedProposalSourceVersion,
    string CorrelationId,
    DateTimeOffset StartedAtUtc,
    string RedactionState = "metadata_only",
    string RetentionClass = "collaboration_input",
    string SchemaVersion = "chatbot.low-risk-ai-assistance-execution-started.v1") : IEventPayload;

public sealed record LowRiskAiAssistanceExecutionSucceeded(
    LowRiskAiAssistanceExecutionRecord Record) : IEventPayload;

public sealed record LowRiskAiAssistanceRoutedToApproval(
    LowRiskAiAssistanceExecutionRecord Record) : IEventPayload;

public sealed record LowRiskAiAssistanceExecutionFailed(
    LowRiskAiAssistanceExecutionRecord Record) : IEventPayload;
