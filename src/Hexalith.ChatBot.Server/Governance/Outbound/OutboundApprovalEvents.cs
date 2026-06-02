using Hexalith.ChatBot.Contracts.Commands;
using Hexalith.ChatBot.Contracts.Enums;
using Hexalith.ChatBot.Contracts.Identities;
using Hexalith.EventStore.Contracts.Events;

namespace Hexalith.ChatBot.Server.Governance.Outbound;

public sealed record OutboundApprovalRequested(
    string ApprovalId,
    string DraftId,
    string ProjectId,
    string RequesterId,
    string RequesterActorType,
    string? SourceConversationId,
    string? SourceMessageId,
    string? SourceConversationItemId,
    IReadOnlyList<string> RecipientRefs,
    IReadOnlyList<string> ContextRefs,
    string PolicySnapshotId,
    string PolicySnapshotVisibility,
    string CommandName,
    string CommandAllowlistVersion,
    OutboundApprovalContentSnapshot ContentSnapshot,
    SenderAuthorityClass SenderAuthorityClass,
    ApprovalEvidenceFreshness EvidenceFreshness,
    string ExpectedPostStateRedactionState,
    long ExpectedDraftSourceVersion,
    long SourceVersion,
    DateTimeOffset RequestedAtUtc,
    string CorrelationId,
    string RedactionState = "metadata_only",
    string RetentionClass = "collaboration_input",
    string SchemaVersion = "chatbot.outbound-approval-requested.v1") : IEventPayload;

public sealed record OutboundApprovalDecisionRecorded(
    string ApprovalId,
    string DraftId,
    string ProjectId,
    ApprovalDecisionKind DecisionKind,
    string DecisionActorId,
    string DecisionActorType,
    DateTimeOffset DecidedAtUtc,
    long ExpectedApprovalSourceVersion,
    string AuthorityResult,
    string? DisabledReason,
    string DecisionRationaleRedactionState,
    string AuditOperationId,
    string AuditStatus,
    string PolicySnapshotId,
    string SafeNextAction,
    OutboundApprovalContentSnapshot ContentSnapshot,
    long SourceVersion,
    string CorrelationId,
    string RedactionState = "metadata_only",
    string RetentionClass = "collaboration_input",
    string SchemaVersion = "chatbot.outbound-approval-decision-recorded.v1") : IEventPayload;

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

public sealed record OutboundApprovalDecisionRejected(
    string ApprovalId,
    string DraftId,
    string ReasonCode,
    long? ExpectedApprovalSourceVersion,
    string CorrelationId) : IRejectionEvent;

public sealed record OutboundApprovalRequestRejected(
    string ApprovalId,
    string DraftId,
    string ProjectId,
    string ReasonCode,
    string CorrelationId) : IRejectionEvent;

public sealed record OutboundSendStarted(
    string SendId,
    string SendKey,
    string ApprovalId,
    string DraftId,
    string ProjectId,
    string RequesterId,
    string SendActorId,
    SenderAuthorityClass SenderAuthorityClass,
    SenderAuthorityClassificationResult AuthorityResult,
    string AdapterMode,
    string AdapterRef,
    long ExpectedApprovalSourceVersion,
    long ExpectedDraftSourceVersion,
    DateTimeOffset StartedAtUtc,
    string CorrelationId,
    string RedactionState = "metadata_only",
    string RetentionClass = "collaboration_input",
    string SchemaVersion = "chatbot.outbound-send-started.v1") : IEventPayload;

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

