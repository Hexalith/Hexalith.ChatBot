using Hexalith.ChatBot.Contracts.Enums;
using Hexalith.EventStore.Contracts.Events;

namespace Hexalith.ChatBot.Server.Governance.AiMediation;

public sealed record AiActionApprovalDecisionRejected(
    string ApprovalId,
    string ProposalId,
    string ReasonCode,
    long? ExpectedApprovalSourceVersion,
    string CorrelationId) : IRejectionEvent;
