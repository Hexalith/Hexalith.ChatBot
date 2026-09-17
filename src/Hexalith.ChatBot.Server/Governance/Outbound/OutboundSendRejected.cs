using Hexalith.ChatBot.Contracts.Commands;
using Hexalith.ChatBot.Contracts.Enums;
using Hexalith.ChatBot.Contracts.Identities;
using Hexalith.EventStore.Contracts.Events;

namespace Hexalith.ChatBot.Server.Governance.Outbound;

public sealed record OutboundSendRejected(
    string SendId,
    string ApprovalId,
    string DraftId,
    string ProjectId,
    string RequesterId,
    string SendActorId,
    string ReasonCode,
    string CorrelationId,
    long? ExpectedApprovalSourceVersion = null,
    long? ExpectedDraftSourceVersion = null,
    string RedactionState = "metadata_only",
    string RetentionClass = "collaboration_input") : IRejectionEvent;
