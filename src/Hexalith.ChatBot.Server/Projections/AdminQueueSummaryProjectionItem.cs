using Hexalith.ChatBot.Contracts.Enums;

namespace Hexalith.ChatBot.Server.Projections;

internal sealed record AdminQueueSummaryProjectionItem(
    string QueueRef,
    string ItemRef,
    string Status,
    string OwnerClass,
    ChatBotHealthStatus Health,
    int AgeSeconds,
    OperationalQueueFamily QueueFamily = OperationalQueueFamily.RetryableOperation,
    string Risk = "medium",
    decimal Confidence = 0.5m,
    string? AssigneeRef = null,
    string NextAction = "review",
    int RetryCount = 0,
    bool IsTerminal = false,
    DateTimeOffset? FreshnessTimestampUtc = null,
    string OwnerRole = "operations-admin",
    string? MailboxRef = null,
    string? FailureState = null,
    long SourceVersion = 0,
    decimal PriorityScore = 0,
    string PriorityExplanation = "stable-order",
    string? ProjectName = null,
    string? EvidenceContent = null,
    string? FileMetadata = null,
    string? AuditReason = null,
    string? MailboxSubject = null,
    string? CandidateEvidence = null);
