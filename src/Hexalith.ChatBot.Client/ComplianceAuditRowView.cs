using System.Net.Http.Json;
using System.Text.Json;

namespace Hexalith.ChatBot.Client;

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
