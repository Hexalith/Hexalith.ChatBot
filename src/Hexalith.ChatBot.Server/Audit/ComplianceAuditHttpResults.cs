using Hexalith.ChatBot.Contracts.Commands;
using Hexalith.ChatBot.Contracts.Enums;
using Hexalith.ChatBot.Contracts.Queries;

namespace Hexalith.ChatBot.Server.Audit;

/// <summary>
/// Story 9.3 (S9): projects the metadata-only compliance audit search/detail contracts onto stable string-token wire
/// models for the investigation surface. Redaction and escalation states are emitted as their kebab tokens (never raw
/// enum ordinals) so the UI timeline can render safe <c>redaction:…</c> / <c>escalation:…</c> tokens without any
/// enum-serialization ambiguity, and every field stays a bounded <see cref="AuditMetadata"/>-safe token (NFR2).
/// </summary>
internal static class ComplianceAuditHttpResults
{
    private static readonly System.Text.Json.JsonSerializerOptions JsonOptions = new(System.Text.Json.JsonSerializerDefaults.Web);

    public static IResult SearchOk(ComplianceAuditSearchResult result)
    {
        ArgumentNullException.ThrowIfNull(result);

        ComplianceAuditSearchWireModel model = ToSearchWire(result);

        return Results.Json(model, statusCode: StatusCodes.Status200OK);
    }

    public static IResult DetailOk(ComplianceAuditDetail detail)
    {
        ArgumentNullException.ThrowIfNull(detail);

        ComplianceAuditDetailWireModel model = ToDetailWire(detail);

        return Results.Json(model, statusCode: StatusCodes.Status200OK);
    }

    public static System.Text.Json.JsonElement SearchJsonElement(ComplianceAuditSearchResult result)
    {
        ArgumentNullException.ThrowIfNull(result);
        return System.Text.Json.JsonSerializer.SerializeToElement(ToSearchWire(result), JsonOptions);
    }

    public static System.Text.Json.JsonElement DetailJsonElement(ComplianceAuditDetail detail)
    {
        ArgumentNullException.ThrowIfNull(detail);
        return System.Text.Json.JsonSerializer.SerializeToElement(ToDetailWire(detail), JsonOptions);
    }

    private static ComplianceAuditSearchWireModel ToSearchWire(ComplianceAuditSearchResult result)
        => new(
            result.QueryRef,
            [.. result.Rows.Select(ToRow)],
            result.ResultFingerprint,
            result.GeneratedAtUtc,
            result.CorrelationId);

    private static ComplianceAuditDetailWireModel ToDetailWire(ComplianceAuditDetail detail)
        => new(
            detail.AuditRecordRef,
            detail.CommandRef,
            detail.ResourceRef,
            detail.CorrelationId,
            detail.RecordedAtUtc,
            detail.PolicySnapshotId,
            RedactionToken(detail.RedactionState),
            EscalationToken(detail.EscalationStatus),
            [.. detail.VisibleMetadataRefs],
            detail.SafeNextAction,
            detail.RedactionReasonCode);

    private static ComplianceAuditRowWireModel ToRow(ComplianceAuditResultRow row)
        => new(
            row.AuditRecordRef,
            row.ActorRef,
            row.ActorType,
            row.CommandRef,
            row.ResourceRef,
            row.Decision,
            row.ReasonCode,
            row.CorrelationId,
            row.RecordedAtUtc,
            row.PolicySnapshotId,
            RedactionToken(row.RedactionState),
            EscalationToken(row.EscalationStatus),
            row.SafeNextAction);

    private static string RedactionToken(ComplianceAuditRedactionState state)
        => state switch
        {
            ComplianceAuditRedactionState.MetadataOnly => "metadata-only",
            ComplianceAuditRedactionState.DetailAvailable => "detail-available",
            ComplianceAuditRedactionState.Restricted => "restricted",
            ComplianceAuditRedactionState.EscalationRequired => "escalation-required",
            _ => "unknown",
        };

    private static string EscalationToken(ComplianceEscalationStatus status)
        => status switch
        {
            ComplianceEscalationStatus.NotRequested => "not-requested",
            ComplianceEscalationStatus.Requested => "requested",
            ComplianceEscalationStatus.Approved => "approved",
            ComplianceEscalationStatus.Denied => "denied",
            _ => "unknown",
        };

    private sealed record ComplianceAuditSearchWireModel(
        string QueryRef,
        IReadOnlyList<ComplianceAuditRowWireModel> Rows,
        string ResultFingerprint,
        DateTimeOffset GeneratedAtUtc,
        string CorrelationId);

    private sealed record ComplianceAuditRowWireModel(
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

    private sealed record ComplianceAuditDetailWireModel(
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
        string RedactionReasonCode);
}
