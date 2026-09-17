using Hexalith.ChatBot.Contracts.Commands;
using Hexalith.ChatBot.Contracts.Enums;
using Hexalith.ChatBot.Contracts.Identities;
using Hexalith.EventStore.Contracts.Events;

namespace Hexalith.ChatBot.Server.Governance.Outbound;

public sealed record OutboundApprovalDecisionRejected(
    string ApprovalId,
    string DraftId,
    string ReasonCode,
    long? ExpectedApprovalSourceVersion,
    string CorrelationId) : IRejectionEvent;
