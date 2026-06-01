using Hexalith.ChatBot.Server.Governance.AiMediation;

namespace Hexalith.ChatBot.Server.Projections;

internal sealed record PublishedAiActionExecutionEvent(
    string? TenantId,
    string? Domain,
    string? AggregateId,
    string? EventTypeName,
    long SequenceNumber,
    DateTimeOffset Timestamp,
    string? CorrelationId,
    ApprovedAiActionExecutionStarted? Started = null,
    ApprovedAiActionExecutionSucceeded? Succeeded = null,
    ApprovedAiActionExecutionFailed? Failed = null,
    ApprovedAiActionExecutionRejected? Rejected = null,
    AiActionProposalInvalidatedByCorrection? Invalidated = null);
