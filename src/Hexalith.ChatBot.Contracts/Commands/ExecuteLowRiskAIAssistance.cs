using Hexalith.ChatBot.Contracts.Enums;
using Hexalith.ChatBot.Contracts.Queries;

namespace Hexalith.ChatBot.Contracts.Commands;

public sealed record ExecuteLowRiskAIAssistance(
    string ProjectId,
    string ProposalId,
    string TaskIntentId,
    string SourceMessageId,
    string RequesterId,
    LowRiskAiAssistanceKind AssistanceKind,
    string ContextPackageId,
    string ContextPackageVersion,
    string ContextPackageRedactionState,
    string RetentionClass,
    string ProviderReuseSetting,
    IReadOnlyList<string> SourceEvidenceReferences,
    IReadOnlyList<string> AuthorizedContextReferences,
    IReadOnlyList<string> ExcludedContextReasons,
    long ExpectedProposalSourceVersion,
    string? PolicySnapshotId,
    string CorrelationId,
    string ExecutionId,
    string TransitionId,
    string? SourceConversationItemId = null,
    AiActionRiskClassificationRecord? RiskClassification = null,
    LowRiskAiAssistanceExecutionRecord? ExecutionRecord = null,
    string RedactionState = "metadata_only",
    string SchemaVersion = "chatbot.low-risk-ai-assistance-execution.v1") : IChatBotCommand;
