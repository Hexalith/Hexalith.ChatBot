using Hexalith.ChatBot.Contracts.Queries;
using Hexalith.ChatBot.Server.Audit;

namespace Hexalith.ChatBot.Server.Observability;

/// <summary>
/// The single tenant-safe, metadata-only operational alert payload shared by all five NFR43 default alert kinds
/// (Story 8.4, NFR42/NFR2). It carries <em>only</em> bounded safe tokens — a stable <see cref="AffectedScope"/>
/// (tenant ref, optionally a safe mailbox ref, never a project name, evidence snippet, file path, actor PII, or
/// audit detail), the <see cref="OwnerRole"/>, the bounded <see cref="NextSafeAction"/>, a stable
/// <see cref="ReasonCode"/>, the <see cref="AlertKind"/>, the correlation id, and the fired-at UTC timestamp. It
/// never carries restricted tenant data, project names, file metadata, candidate evidence, authorization-claim
/// detail, or secrets.
/// </summary>
/// <param name="AlertKind">The closed alert-kind enum (also drives audit routing).</param>
/// <param name="AffectedScope">The safe scope token: <c>tenant:{ref}</c> or <c>tenant:{ref} mailbox:{ref}</c>.</param>
/// <param name="OwnerRole">The owner admin role (hyphen-separated safe token, e.g. <c>operations-admin</c>).</param>
/// <param name="NextSafeAction">A bounded safe next-action token from a closed set, never raw error text.</param>
/// <param name="ReasonCode">A stable underscore-separated reason-code token.</param>
/// <param name="TenantRef">The tenant reference from the authenticated binding (never request body).</param>
/// <param name="CorrelationId">The correlation id for the evaluation pass.</param>
/// <param name="FiredAtUtc">The server-measured UTC instant the alert fired.</param>
internal sealed record OperationalAlertPayload(
    OperatorAlertKind AlertKind,
    string AffectedScope,
    string OwnerRole,
    string NextSafeAction,
    string ReasonCode,
    string TenantRef,
    string CorrelationId,
    DateTimeOffset FiredAtUtc)
{
    /// <summary>
    /// The bounded, marker-safe wire token for an operational alert kind (used as the audit resource ref and the
    /// audit-envelope alert-kind ref). All tokens pass <see cref="OperationalDashboardContractValidator.IsSafeToken"/>.
    /// </summary>
    public static string AlertKindWireValue(OperatorAlertKind kind)
        => kind switch
        {
            OperatorAlertKind.AuditProjectionLagBreached => "audit-projection-lag-breached",
            OperatorAlertKind.RetryExhausted => "retry-exhausted",
            OperatorAlertKind.ApprovalQueueAgeBreached => "approval-queue-age-breached",
            OperatorAlertKind.SubscriptionExpiryImminent => "subscription-expiry-imminent",
            OperatorAlertKind.AuthorizationFailureSpike => "authorization-failure-spike",
            _ => throw new ArgumentOutOfRangeException(nameof(kind), kind, "Unsupported operational alert kind."),
        };

    /// <summary>
    /// Validates the payload against the operational-dashboard ASCII/marker-ban posture (NFR2/NFR42). Returns the
    /// list of validation errors — empty when the payload is a well-formed, tenant-safe, metadata-only alert. Mirrors
    /// the <see cref="OperatingBaselineContractValidator.Validate(PublishedSlo)"/> shape: each safe-token field is
    /// checked, the alert kind must be defined, and the fired-at timestamp must be UTC.
    /// </summary>
    public static IReadOnlyList<string> Validate(OperationalAlertPayload payload)
    {
        ArgumentNullException.ThrowIfNull(payload);

        List<string> errors = [];

        if (!Enum.IsDefined(payload.AlertKind))
        {
            errors.Add("alert_kind_invalid");
        }

        // The affected scope is one or more space-separated components (e.g. "tenant:t mailbox:m"); each component
        // must individually be a safe token — the space itself is the only permitted separator.
        string[] scopeComponents = string.IsNullOrWhiteSpace(payload.AffectedScope)
            ? []
            : payload.AffectedScope.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (scopeComponents.Length == 0 ||
            scopeComponents.Any(static component => !OperationalDashboardContractValidator.IsRequiredSafeToken(component)))
        {
            errors.Add("affected_scope_invalid");
        }

        if (!OperationalDashboardContractValidator.IsRequiredSafeToken(payload.OwnerRole))
        {
            errors.Add("owner_role_invalid");
        }

        if (!OperationalDashboardContractValidator.IsRequiredSafeToken(payload.NextSafeAction))
        {
            errors.Add("next_safe_action_invalid");
        }

        if (!OperationalDashboardContractValidator.IsRequiredSafeToken(payload.ReasonCode))
        {
            errors.Add("reason_code_invalid");
        }

        if (!OperationalDashboardContractValidator.IsRequiredSafeToken(payload.TenantRef))
        {
            errors.Add("tenant_ref_invalid");
        }

        if (!OperationalDashboardContractValidator.IsRequiredSafeToken(payload.CorrelationId))
        {
            errors.Add("correlation_id_invalid");
        }

        if (payload.FiredAtUtc.Offset != TimeSpan.Zero)
        {
            errors.Add("fired_at_not_utc");
        }

        return errors;
    }

    /// <summary>Returns <see langword="true"/> when the payload is a well-formed, tenant-safe, metadata-only alert.</summary>
    public static bool IsValid(OperationalAlertPayload payload)
        => Validate(payload).Count == 0;
}

/// <summary>
/// The result of one operational-alert evaluation pass: how many alerts fired, were delivered, or were suppressed
/// fail-closed because the pre-commit audit was unavailable. Mirrors <c>ReviewerBacklogAlertOutcome</c>.
/// </summary>
internal sealed record OperationalAlertOutcome(int Fired, int Delivered, int AuditUnavailable);
