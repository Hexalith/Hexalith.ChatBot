using Hexalith.ChatBot.Contracts.Commands;
using Hexalith.ChatBot.Contracts.Enums;
using Hexalith.ChatBot.Contracts.Identities;
using Hexalith.EventStore.Contracts.Events;

namespace Hexalith.ChatBot.Server.Governance.Outbound;

public sealed record OutboundSendSucceeded(
    string SendId,
    string SendKey,
    string ApprovalId,
    string DraftId,
    string ProjectId,
    string AdapterRef,
    DateTimeOffset SucceededAtUtc,
    string AuditOperationId,
    string AuditStatus,
    string CorrelationId,
    string RedactionState = "metadata_only",
    string RetentionClass = "collaboration_input",
    string SchemaVersion = "chatbot.outbound-send-succeeded.v1") : IEventPayload;
