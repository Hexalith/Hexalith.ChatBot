using System.Security.Claims;

using Hexalith.ChatBot.Contracts.Commands;
using Hexalith.ChatBot.Contracts.Enums;
using Hexalith.ChatBot.Contracts.Queries;
using Hexalith.ChatBot.Server.Governance.Admin;

namespace Hexalith.ChatBot.Server.Audit;

internal static class ComplianceAuditReadPolicy
{
    public static bool CanSearchTenantAudit(ClaimsPrincipal principal)
        => AdminAuthorityEvaluator.HasHumanAdminScope(principal, AdminScope.Compliance);

    public static ComplianceAuditSearchResult Search(
        ClaimsPrincipal principal,
        ComplianceAuditQueryFilters query,
        IReadOnlyList<AuditEnvelope> envelopes,
        DateTimeOffset generatedAtUtc,
        string correlationId)
    {
        ArgumentNullException.ThrowIfNull(principal);
        ArgumentNullException.ThrowIfNull(query);
        ArgumentNullException.ThrowIfNull(envelopes);

        if (!CanSearchTenantAudit(principal) ||
            !ComplianceAdministrationSchema.ValidateAuditQueryFilters(query).IsValid ||
            !ComplianceAdministrationSchema.IsSafeComplianceToken(correlationId) ||
            !ComplianceAdministrationSchema.IsUtc(generatedAtUtc))
        {
            return new ComplianceAuditSearchResult(
                AuditMetadata.SafeOptionalToken(query.QueryRef) ?? "denied",
                [],
                "sha256:denied",
                generatedAtUtc.ToUniversalTime(),
                AuditMetadata.SafeOptionalToken(correlationId) ?? "denied");
        }

        ComplianceAuditResultRow[] rows = envelopes
            .Where(static envelope => AuditMetadata.IsSafeStableIdentifier(envelope.ResourceId))
            .Where(envelope => envelope.Timestamp >= query.FromUtc && envelope.Timestamp <= query.ToUtc)
            .Where(envelope => query.Filters.All(filter => MatchesFilter(envelope, filter)))
            .OrderBy(static envelope => envelope.Timestamp)
            .Take(query.Limit)
            .Select(static envelope => ToRow(envelope, ComplianceAuditRedactionState.Restricted, ComplianceEscalationStatus.NotRequested, "request-access"))
            .ToArray();

        return new ComplianceAuditSearchResult(
            query.QueryRef,
            rows,
            $"sha256:{rows.Length.ToString(System.Globalization.CultureInfo.InvariantCulture)}",
            generatedAtUtc,
            correlationId);
    }

    public static ComplianceAuditDetail Detail(AuditEnvelope envelope, bool hasPerProjectAuthority)
    {
        ArgumentNullException.ThrowIfNull(envelope);

        ComplianceAuditRedactionState redaction = hasPerProjectAuthority
            ? ComplianceAuditRedactionState.DetailAvailable
            : ComplianceAuditRedactionState.EscalationRequired;
        ComplianceEscalationStatus escalation = hasPerProjectAuthority
            ? ComplianceEscalationStatus.NotRequested
            : ComplianceEscalationStatus.Requested;
        string nextAction = hasPerProjectAuthority ? "view-metadata" : "request-access";

        return new ComplianceAuditDetail(
            SafeIdentifier(envelope.ResourceId),
            AuditMetadata.SafeCommandName(envelope.CommandName),
            SafeIdentifier(envelope.ResourceId),
            SafeIdentifier(envelope.CorrelationId),
            envelope.Timestamp.ToUniversalTime(),
            SafeIdentifier(envelope.PolicySnapshotId),
            redaction,
            escalation,
            hasPerProjectAuthority ? envelope.SourceEvidenceRefs.Where(static value => AuditMetadata.SafeOptionalToken(value) is not null).ToArray() : [],
            nextAction,
            hasPerProjectAuthority ? "metadata-visible" : "restricted-detail");
    }

    private static ComplianceAuditResultRow ToRow(
        AuditEnvelope envelope,
        ComplianceAuditRedactionState redactionState,
        ComplianceEscalationStatus escalationStatus,
        string safeNextAction)
        => new(
            SafeIdentifier(envelope.ResourceId),
            SafeIdentifier(envelope.ActorId),
            AuditMetadata.SafeActorType(envelope.ActorType),
            AuditMetadata.SafeCommandName(envelope.CommandName),
            SafeIdentifier(envelope.ResourceId),
            AuditMetadata.SafeOptionalToken(envelope.Decision) ?? "unknown",
            AuditMetadata.SafeOptionalToken(envelope.ReasonCode) ?? "unknown",
            SafeIdentifier(envelope.CorrelationId),
            envelope.Timestamp.ToUniversalTime(),
            SafeIdentifier(envelope.PolicySnapshotId),
            redactionState,
            escalationStatus,
            safeNextAction);

    private static bool MatchesFilter(AuditEnvelope envelope, ComplianceAuditFilterRef filter)
        => filter.FilterKey switch
        {
            "tenant" => Matches(envelope.TenantId, filter.ValueRef),
            "actor" => Matches(envelope.ActorId, filter.ValueRef),
            "actor-type" => string.Equals(AuditMetadata.SafeActorType(envelope.ActorType), filter.ValueRef, StringComparison.Ordinal),
            "command" => string.Equals(AuditMetadata.SafeCommandName(envelope.CommandName), filter.ValueRef, StringComparison.Ordinal),
            "resource" => Matches(envelope.ResourceId, filter.ValueRef),
            "decision" => string.Equals(AuditMetadata.SafeOptionalToken(envelope.Decision) ?? "unknown", filter.ValueRef, StringComparison.Ordinal),
            "reason" => string.Equals(AuditMetadata.SafeOptionalToken(envelope.ReasonCode) ?? "unknown", filter.ValueRef, StringComparison.Ordinal),
            "correlation" => Matches(envelope.CorrelationId, filter.ValueRef),
            "policy-snapshot" => Matches(envelope.PolicySnapshotId, filter.ValueRef),
            "time" => true,
            _ => false,
        };

    private static bool Matches(string? envelopeValue, string filterValue)
        => string.Equals(SafeIdentifier(envelopeValue), filterValue, StringComparison.Ordinal);

    private static string SafeIdentifier(string? value)
        => AuditMetadata.IsSafeStableIdentifier(value) ? value! : "redacted-ref";
}
