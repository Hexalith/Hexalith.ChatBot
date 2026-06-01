using Hexalith.ChatBot.Server.Governance.AiMediation;

namespace Hexalith.ChatBot.Server.Projections;

internal sealed record PublishedAiActionApprovalEvent(
    string? TenantId,
    string? Domain,
    string? AggregateId,
    string? EventTypeName,
    long SequenceNumber,
    DateTimeOffset Timestamp,
    string? CorrelationId,
    AiActionApprovalRequested? Request = null,
    AiActionApprovalDecisionRecorded? Decision = null);
