using Hexalith.ChatBot.Contracts.Commands;
using Hexalith.ChatBot.Contracts.Enums;
using Hexalith.ChatBot.Contracts.Identities;
using Hexalith.EventStore.Contracts.Events;

namespace Hexalith.ChatBot.Server.Governance.Outbound;

public sealed record OutboundApprovalRequestRejected(
    string ApprovalId,
    string DraftId,
    string ProjectId,
    string ReasonCode,
    string CorrelationId) : IRejectionEvent;
