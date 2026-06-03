using System.Security.Claims;

using Hexalith.ChatBot.Contracts.Commands;
using Hexalith.ChatBot.Contracts.Enums;
using Hexalith.ChatBot.Contracts.Queries;
using Hexalith.ChatBot.Server.Gateway.Stages;
using Hexalith.ChatBot.Server.Governance.Admin;

namespace Hexalith.ChatBot.Server.Audit;

internal static class ComplianceAuditReadPolicy
{
    public static bool CanSearchTenantAudit(ClaimsPrincipal principal)
        => AdminAuthorityEvaluator.HasHumanAdminScope(principal, AdminScope.Compliance);

    /// <summary>
    /// Story 9.3 (AC2, NFR2, Flow 7): per-project detail authority is evaluated against the reviewer's <b>actual</b>
    /// grants, never assumed true. A tenant-wide <see cref="AdminScope.Compliance"/> reviewer can see redacted rows,
    /// but full detail for a project's records requires an explicit per-project owner grant matching a
    /// <c>project:</c> evidence token on the record. Absent that grant the surface renders the safe
    /// <see cref="ComplianceAuditRedactionState.EscalationRequired"/> state and a <c>request-access</c> next action —
    /// it never leaks the restricted project, file metadata, candidate evidence, or audit detail.
    /// </summary>
    public static bool HasPerProjectAuthority(ClaimsPrincipal principal, AuditEnvelope envelope)
    {
        ArgumentNullException.ThrowIfNull(principal);
        ArgumentNullException.ThrowIfNull(envelope);

        if (!CanSearchTenantAudit(principal))
        {
            return false;
        }

        HashSet<string> grantedProjects = principal
            .FindAll(ParticipantAuthorizationStage.ProjectOwnerClaim)
            .Select(static claim => claim.Value)
            .Where(AuditMetadata.IsSafeStableIdentifier)
            .ToHashSet(StringComparer.Ordinal);

        return grantedProjects.Count != 0 &&
            envelope.SourceEvidenceRefs
                .Where(static reference => reference.StartsWith("project:", StringComparison.Ordinal))
                .Select(static reference => reference["project:".Length..])
                .Any(grantedProjects.Contains);
    }

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
            // Story 9.3 (FR95a): replay/simulation records are excluded from default production audit queries. The
            // exclusion composes with the safe-identifier, time-window, and filter predicates so a replay-marked
            // record can never appear in a default result. Story 9.4 owns populating ReplayRunId; today production
            // holds zero replay records, so the exclusion holds by construction yet is real and testable now. Reuse
            // the Story 9.2 predicate rather than re-deriving the marker test.
            .Where(static envelope => !AuditReplayExclusion.IsReplayEnvelope(envelope))
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
            // Story 9.3 (FR56): surface origin matches the envelope's safe SurfaceOrigin token; message id matches a
            // source-message:/provider-message: token in the source-evidence refs (the value is treated as an opaque
            // safe token, never raw content). These two arms must stay in lock-step with
            // ComplianceAdministrationSchema.AuditFilterKeys.
            "surface" => string.Equals(AuditMetadata.SafeOptionalToken(envelope.SurfaceOrigin), filter.ValueRef, StringComparison.Ordinal),
            "message-id" => MatchesMessageId(envelope, filter.ValueRef),
            "time" => true,
            _ => false,
        };

    private static bool MatchesMessageId(AuditEnvelope envelope, string filterValue)
        => AuditMetadata.SafeOptionalToken(filterValue) is { } safeValue &&
            (envelope.SourceEvidenceRefs.Contains($"source-message:{safeValue}", StringComparer.Ordinal) ||
                envelope.SourceEvidenceRefs.Contains($"provider-message:{safeValue}", StringComparer.Ordinal));

    private static bool Matches(string? envelopeValue, string filterValue)
        => string.Equals(SafeIdentifier(envelopeValue), filterValue, StringComparison.Ordinal);

    private static string SafeIdentifier(string? value)
        => AuditMetadata.IsSafeStableIdentifier(value) ? value! : "redacted-ref";
}
