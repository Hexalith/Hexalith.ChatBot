using Hexalith.ChatBot.Contracts.Enums;

namespace Hexalith.ChatBot.Server.Projections;

internal sealed record AdminQueueSummaryProjectionItem(
    string QueueRef,
    string ItemRef,
    string Status,
    string OwnerClass,
    ChatBotHealthStatus Health,
    int AgeSeconds,
    string? ProjectName = null,
    string? EvidenceContent = null,
    string? FileMetadata = null,
    string? AuditReason = null,
    string? MailboxSubject = null,
    string? CandidateEvidence = null);
