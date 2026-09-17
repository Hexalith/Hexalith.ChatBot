using Hexalith.EventStore.Contracts.Events;

namespace Hexalith.ChatBot.Server.Governance.AiMediation;

public sealed record AiActionProposalInvalidationRejected(
    string ProposalId,
    string? ApprovalId,
    string ReasonCode,
    long? EvidenceSnapshotSourceVersion,
    string CorrelationId) : IRejectionEvent;
