using Hexalith.ChatBot.Contracts.Enums;
using Hexalith.EventStore.Contracts.Events;

namespace Hexalith.ChatBot.Server.Governance.AiMediation;

public sealed record AiActionApprovalDecisionRecorded(
    string ApprovalId,
    string ProjectId,
    string ProposalId,
    string SourceMessageId,
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
    long SourceVersion,
    string CorrelationId,
    string RedactionState = "metadata_only",
    string RetentionClass = "collaboration_input",
    string SchemaVersion = "chatbot.ai-action-approval-decision-recorded.v1") : IEventPayload;
