using Hexalith.ChatBot.Contracts.Enums;

namespace Hexalith.ChatBot.Contracts.Commands;

public sealed record DecideAiActionApproval(
    string ProjectId,
    string ApprovalId,
    string ProposalId,
    string SourceMessageId,
    ApprovalDecisionKind Decision,
    long ExpectedApprovalSourceVersion,
    string CorrelationId,
    string DecisionId,
    string RationaleRedactionState = "metadata_only",
    string SchemaVersion = "chatbot.ai-action-approval-decision.v1") : IChatBotCommand;
