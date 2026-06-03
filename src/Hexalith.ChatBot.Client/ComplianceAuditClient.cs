using System.Net.Http.Json;
using System.Text.Json;

namespace Hexalith.ChatBot.Client;

/// <summary>A metadata-only compliance audit query sent to the S9 investigation search endpoint (Story 9.3).</summary>
public sealed record ComplianceAuditQuery(
    string QueryRef,
    IReadOnlyList<ComplianceAuditFilter> Filters,
    DateTimeOffset FromUtc,
    DateTimeOffset ToUtc,
    int Limit);

/// <summary>A single audit query filter dimension (e.g. <c>actor</c>, <c>surface</c>, <c>message-id</c>).</summary>
public sealed record ComplianceAuditFilter(string FilterRef, string FilterKey, string ValueRef);

/// <summary>The metadata-only result of a compliance audit search — bounded safe tokens only.</summary>
public sealed record ComplianceAuditSearchView(
    string QueryRef,
    IReadOnlyList<ComplianceAuditRowView> Rows,
    string ResultFingerprint,
    DateTimeOffset GeneratedAtUtc,
    string CorrelationId)
{
    public static ComplianceAuditSearchView Denied { get; } = new("denied", [], "sha256:denied", default, "denied");
}

/// <summary>A single metadata-only audit timeline row.</summary>
public sealed record ComplianceAuditRowView(
    string AuditRecordRef,
    string ActorRef,
    string ActorType,
    string CommandRef,
    string ResourceRef,
    string Decision,
    string ReasonCode,
    string CorrelationId,
    DateTimeOffset RecordedAtUtc,
    string PolicySnapshotId,
    string RedactionState,
    string EscalationStatus,
    string SafeNextAction);

/// <summary>The metadata-only detail of a single audit record (redacted unless per-project authority is held).</summary>
public sealed record ComplianceAuditDetailView(
    string AuditRecordRef,
    string CommandRef,
    string ResourceRef,
    string CorrelationId,
    DateTimeOffset RecordedAtUtc,
    string PolicySnapshotId,
    string RedactionState,
    string EscalationStatus,
    IReadOnlyList<string> VisibleMetadataRefs,
    string SafeNextAction,
    string RedactionReasonCode)
{
    public static ComplianceAuditDetailView Restricted { get; } = new(
        "redacted-ref", "unknown", "redacted-ref", "redacted-ref", default, "redacted-ref",
        "escalation-required", "requested", [], "request-access", "restricted-detail");
}
