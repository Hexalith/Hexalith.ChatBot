using System.Globalization;

using Hexalith.ChatBot.Client;
using Hexalith.ChatBot.Contracts.Commands;
using Hexalith.ChatBot.Contracts.Enums;

using CommandSubmissionResponse = Hexalith.ChatBot.Client.Generated.CommandSubmissionResponse;

namespace Hexalith.ChatBot.UI.Services;

/// <summary>
/// UI-owned read/escalate-only service for the S9 compliance audit investigation surface (Story 9.3). It reads the
/// tenant WORM audit chain as metadata-only safe tokens through <see cref="IChatBotClient"/> and dispatches the
/// already-allowlisted <see cref="RequestComplianceInvestigation"/> / <see cref="RequestComplianceEscalation"/>
/// commands (which record intent — they are not workflow-item mutations). It never touches Server projections,
/// stores, the read policy, or the audit-record types directly, and exposes no affordance that mutates workflow state.
/// </summary>
public sealed class ComplianceAuditService(IChatBotClient client)
{
    private const string SchemaVersion = ComplianceAdministrationSchemaVersions.V1;
    private readonly IChatBotClient _client = client ?? throw new ArgumentNullException(nameof(client));

    public async Task<ComplianceAuditTimelineModel> SearchAsync(
        ComplianceAuditQueryModel query,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);

        ComplianceAuditSearchView view = await _client
            .SearchComplianceAuditRecordsAsync(BuildQuery(query), cancellationToken: cancellationToken)
            .ConfigureAwait(false);

        return new ComplianceAuditTimelineModel(
            view.QueryRef,
            [.. view.Rows.Select(MapRow)],
            view.ResultFingerprint,
            view.GeneratedAtUtc);
    }

    public async Task<ComplianceAuditDetailModel> GetDetailAsync(
        string auditRecordRef,
        CancellationToken cancellationToken = default)
    {
        ComplianceAuditDetailView view = await _client
            .GetComplianceAuditDetailAsync(auditRecordRef, cancellationToken: cancellationToken)
            .ConfigureAwait(false);

        return new ComplianceAuditDetailModel(
            view.AuditRecordRef,
            view.CommandRef,
            view.CorrelationId,
            view.PolicySnapshotId,
            view.RedactionState,
            view.EscalationStatus,
            view.SafeNextAction,
            view.RedactionReasonCode,
            [.. view.VisibleMetadataRefs]);
    }

    public async Task<ComplianceCommandResult> RequestEscalationAsync(
        string auditRecordRef,
        string investigationId,
        string escalationTarget,
        CancellationToken cancellationToken = default)
    {
        RequestComplianceEscalation command = new(
            EscalationId: $"escalation:{auditRecordRef}",
            InvestigationId: investigationId,
            AuditRecordRef: auditRecordRef,
            ReasonCode: "compliance-escalation",
            RequesterRef: "compliance-reviewer",
            EscalationTargetRef: escalationTarget,
            SourceVersion: 0,
            CorrelationId: "01ARZ3NDEKTSV4RRFFQ69G5FAW",
            PolicySnapshotId: "policy-snapshot-compliance-v1",
            RedactionState: ComplianceAuditRedactionState.EscalationRequired,
            EscalationStatus: ComplianceEscalationStatus.Requested,
            SchemaVersion: SchemaVersion);

        return await SubmitCommandAsync(command, cancellationToken).ConfigureAwait(false);
    }

    public async Task<ComplianceCommandResult> TriggerInvestigationAsync(
        string investigationId,
        string queryRef,
        IReadOnlyList<string> filterRefs,
        CancellationToken cancellationToken = default)
    {
        RequestComplianceInvestigation command = new(
            InvestigationId: investigationId,
            QueryRef: queryRef,
            FilterRefs: filterRefs ?? [],
            ReasonCode: "compliance-investigation",
            RequesterRef: "compliance-reviewer",
            SourceVersion: 0,
            CorrelationId: "01ARZ3NDEKTSV4RRFFQ69G5FAW",
            PolicySnapshotId: "policy-snapshot-compliance-v1",
            RedactionState: ComplianceAuditRedactionState.MetadataOnly,
            EscalationStatus: ComplianceEscalationStatus.NotRequested,
            SchemaVersion: SchemaVersion);

        return await SubmitCommandAsync(command, cancellationToken).ConfigureAwait(false);
    }

    private async Task<ComplianceCommandResult> SubmitCommandAsync(IChatBotCommand command, CancellationToken cancellationToken)
    {
        CommandSubmissionResponse accepted = await _client
            .SubmitAsync(command, origin: ChatBotSurfaceOrigin.Ui, cancellationToken: cancellationToken)
            .ConfigureAwait(false);

        return new ComplianceCommandResult(accepted.CommandId, accepted.CorrelationId, accepted.TaskId);
    }

    private static ComplianceAuditQuery BuildQuery(ComplianceAuditQueryModel query)
    {
        // A `time` baseline keeps the query valid (the schema requires at least one filter) and matches every record
        // inside the UTC window; the user's dimension filters narrow it from there.
        List<ComplianceAuditFilter> filters = [new ComplianceAuditFilter("audit-filter-time", "time", "all")];
        AddFilter(filters, "tenant", query.Tenant);
        AddFilter(filters, "actor", query.Actor);
        AddFilter(filters, "command", query.Command);
        AddFilter(filters, "resource", query.Resource);
        AddFilter(filters, "decision", query.Decision);
        AddFilter(filters, "reason", query.Reason);
        AddFilter(filters, "correlation", query.Correlation);
        AddFilter(filters, "message-id", query.MessageId);
        AddFilter(filters, "surface", query.Surface);

        return new ComplianceAuditQuery(
            "audit-query-s9",
            filters,
            query.FromUtc,
            query.ToUtc,
            query.Limit <= 0 ? 100 : query.Limit);
    }

    private static void AddFilter(List<ComplianceAuditFilter> filters, string key, string? value)
    {
        if (!string.IsNullOrWhiteSpace(value))
        {
            filters.Add(new ComplianceAuditFilter($"audit-filter-{key}", key, value.Trim()));
        }
    }

    private static ComplianceAuditRowModel MapRow(ComplianceAuditRowView row)
        => new(
            row.AuditRecordRef,
            row.ActorRef,
            row.ActorType,
            row.CommandRef,
            row.Decision,
            row.ReasonCode,
            row.CorrelationId,
            row.PolicySnapshotId,
            row.RedactionState,
            row.EscalationStatus,
            row.SafeNextAction,
            row.RecordedAtUtc.ToUniversalTime().ToString("yyyy-MM-dd HH:mm:ssZ", CultureInfo.InvariantCulture));
}

/// <summary>The FR56 query dimensions captured by the surface's labelled filter controls.</summary>
public sealed record ComplianceAuditQueryModel(
    string? Tenant,
    string? Actor,
    string? Command,
    string? Resource,
    string? Decision,
    string? Reason,
    string? Correlation,
    string? MessageId,
    string? Surface,
    DateTimeOffset FromUtc,
    DateTimeOffset ToUtc,
    int Limit);

public sealed record ComplianceAuditTimelineModel(
    string QueryRef,
    IReadOnlyList<ComplianceAuditRowModel> Rows,
    string ResultFingerprint,
    DateTimeOffset GeneratedAtUtc);

public sealed record ComplianceAuditRowModel(
    string AuditRecordRef,
    string Actor,
    string ActorType,
    string Command,
    string Decision,
    string Reason,
    string Correlation,
    string PolicySnapshot,
    string Redaction,
    string Escalation,
    string SafeNextAction,
    string TimestampZ);

public sealed record ComplianceAuditDetailModel(
    string AuditRecordRef,
    string Command,
    string Correlation,
    string PolicySnapshot,
    string Redaction,
    string Escalation,
    string SafeNextAction,
    string RedactionReasonCode,
    IReadOnlyList<string> VisibleMetadataRefs);

public sealed record ComplianceCommandResult(string CommandId, string CorrelationId, string? TaskId);
