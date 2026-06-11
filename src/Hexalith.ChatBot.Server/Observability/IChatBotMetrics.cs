namespace Hexalith.ChatBot.Server.Observability;

/// <summary>
/// The bounded, metadata-only emission seam for the FR94 operational metrics (Story 8.2). Each method takes only
/// the authenticated bound tenant id (from <c>ChatBotTenantBinding</c>) and, for latency classes, a server-side
/// millisecond duration; the operation-class is a fixed finite token per method. No payloads, evidence, free-form
/// tag bags, correlation/operation ids, or any high-cardinality value may be passed — correlation is satisfied by
/// the active trace/span, never a metric label. Every implementation MUST be non-blocking: an emission failure is
/// swallowed (never propagated into the operation path) and recorded on the gap-detection meta-counter instead.
/// </summary>
internal interface IChatBotMetrics
{
    /// <summary>Records mailbox-intake (ingestion) latency in milliseconds. Operation-class <c>message-intake</c>.</summary>
    void RecordIngestionLatency(string tenantId, double milliseconds);

    /// <summary>Records association-scoring latency in milliseconds. Operation-class <c>association</c>.</summary>
    void RecordAssociationLatency(string tenantId, double milliseconds);

    /// <summary>Records approval-decision latency in milliseconds. Operation-class <c>approval</c>.</summary>
    void RecordApprovalLatency(string tenantId, double milliseconds);

    /// <summary>Records command-execution dispatch latency in milliseconds. Operation-class <c>command-execution</c>.</summary>
    void RecordCommandExecutionLatency(string tenantId, double milliseconds);

    /// <summary>Increments the retry-exhaustion counter for a workflow item that reached the retry-exhausted terminal state. Operation-class <c>retry</c>.</summary>
    void RecordRetryExhausted(string tenantId);

    /// <summary>Increments the duplicate-suppression counter when a duplicate provider message is suppressed. Operation-class <c>duplicate-handling</c>.</summary>
    void RecordDuplicateSuppressed(string tenantId);

    /// <summary>Increments a bounded workflow lifecycle counter. Operation-class <c>workflow</c>.</summary>
    void RecordWorkflowLifecycle(string tenantId, string status, string reason);
}
