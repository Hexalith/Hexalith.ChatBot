using Hexalith.ChatBot.Contracts.Enums;

namespace Hexalith.ChatBot.Contracts.Commands;

public sealed record DecideOutboundApproval(
    string ApprovalId,
    string DraftId,
    string ProjectId,
    ApprovalDecisionKind Decision,
    string DecisionId,
    long ExpectedApprovalSourceVersion,
    string CorrelationId,
    OutboundDraftContent? ApprovedContent = null,
    string DecisionRationaleRedactionState = "metadata_only",
    string SchemaVersion = "chatbot.outbound-approval-decision.v1") : IChatBotCommand;

