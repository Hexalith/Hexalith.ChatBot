using System.Net.Http.Json;
using System.Text.Json;

namespace Hexalith.ChatBot.Client;

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
