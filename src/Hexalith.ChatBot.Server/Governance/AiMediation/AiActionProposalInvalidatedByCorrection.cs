using Hexalith.EventStore.Contracts.Events;

namespace Hexalith.ChatBot.Server.Governance.AiMediation;

public sealed record AiActionProposalInvalidatedByCorrection(
    string ProposalId,
    string? ApprovalId,
    string TaskIntentId,
    string SourceMessageId,
    string? SourceConversationItemId,
    string RequesterId,
    string ProjectId,
    string AssociationId,
    string CorrectionId,
    string CorrectedEvidenceState,
    long EvidenceSnapshotSourceVersion,
    string CorrelationId,
    string RedactionState,
    string RetentionClass,
    string SchemaVersion = "chatbot.ai-action-proposal-invalidated-by-correction.v1") : IEventPayload;

public sealed record AiActionProposalInvalidationRejected(
    string ProposalId,
    string? ApprovalId,
    string ReasonCode,
    long? EvidenceSnapshotSourceVersion,
    string CorrelationId) : IRejectionEvent;
