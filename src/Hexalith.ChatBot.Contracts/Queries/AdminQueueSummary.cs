using Hexalith.ChatBot.Contracts.Enums;
using Hexalith.ChatBot.Contracts.Identities;

namespace Hexalith.ChatBot.Contracts.Queries;

/// <summary>
/// Summary-safe tenant-admin queue projection. It intentionally omits project, evidence, file, mailbox, and audit-detail fields.
/// </summary>
public sealed record AdminQueueSummary(
    string QueueRef,
    ChatBotHealthStatus Health,
    IReadOnlyList<AdminQueueSummaryBucket> Buckets,
    IReadOnlyList<AdminQueueSummaryItemRef> VisibleItemRefs,
    AdminOperationReference AuditRef,
    string SchemaVersion,
    string CorrelationId);
