using Hexalith.ChatBot.Contracts.Commands;
using Hexalith.ChatBot.Contracts.Enums;
using Hexalith.ChatBot.Contracts.Identities;
using Hexalith.EventStore.Contracts.Events;

namespace Hexalith.ChatBot.Server.Governance.Outbound;

public sealed record OutboundApprovalOutcomeRecorded(
    string ApprovalId,
    string DraftId,
    string ProjectId,
    ApprovalStatus Status,
    string CommandOutcomeStatus,
    DateTimeOffset OutcomeAtUtc,
    string AuditOperationId,
    string AuditStatus,
    string? FailureCode,
    string? Retryability,
    long SourceVersion,
    string CorrelationId,
    string RedactionState = "metadata_only",
    string RetentionClass = "collaboration_input",
    string SchemaVersion = "chatbot.outbound-approval-outcome-recorded.v1") : IEventPayload;
