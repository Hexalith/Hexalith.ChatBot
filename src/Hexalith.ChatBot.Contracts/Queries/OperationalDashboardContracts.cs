using Hexalith.ChatBot.Contracts.Enums;

namespace Hexalith.ChatBot.Contracts.Queries;

/// <summary>
/// Read-only request for the M2 operational dashboard overview (S8/S10, FR67). It carries no project/tenant id —
/// tenant identity comes from the authenticated gateway binding only — and never mutates state. It is a query,
/// not an <c>IChatBotCommand</c>: it adds no write path and no allowlist entry.
/// </summary>
/// <param name="ScopeUsed">The admin see-only scope the caller reads under.</param>
/// <param name="CorrelationId">The correlation identity carried through the spine.</param>
/// <param name="AggregationLimit">The maximum number of source rows aggregated per view.</param>
public sealed record GetOperationalDashboard(
    AdminScope ScopeUsed,
    string CorrelationId,
    int AggregationLimit);

/// <summary>
/// Summary-safe, metadata-only multi-view health overview. Every view renders the stable status enum (never a
/// count-derived status), an optional depth, the oldest item age, the owner role for triage, a detail-link state,
/// and a bounded-staleness freshness timestamp + state.
/// </summary>
/// <param name="Views">One row per FR67 observability view, plus audit projection lag.</param>
/// <param name="FreshnessTimestampUtc">The overall snapshot instant (UTC) for the overview.</param>
/// <param name="FreshnessState">The overall bounded-staleness freshness state.</param>
/// <param name="SchemaVersion">The stable schema version token.</param>
/// <param name="CorrelationId">The correlation identity carried through the spine.</param>
public sealed record OperationalDashboardOverview(
    IReadOnlyList<OperationalDashboardView> Views,
    DateTimeOffset FreshnessTimestampUtc,
    ChatBotFreshnessState FreshnessState,
    string SchemaVersion,
    string CorrelationId);

/// <summary>
/// A single metadata-only dashboard view row. <see cref="Depth"/> is the optional current queue depth; it is a
/// display count only and is never the basis for <see cref="Health"/>, which is the stable health enum surfaced
/// by the underlying source. Restricted per-item detail stays behind the existing authorized hydration path —
/// <see cref="DetailLinkState"/> conveys a safe open/request-access/disabled state without existence leakage.
/// </summary>
/// <param name="View">The observability view identity.</param>
/// <param name="Health">The stable health enum (never derived from counts).</param>
/// <param name="Depth">The optional current queue depth (display count), or <see langword="null"/>.</param>
/// <param name="OldestItemAgeSeconds">The age of the oldest contributing item, in seconds.</param>
/// <param name="OwnerRole">The owner role responsible for triage.</param>
/// <param name="FreshnessTimestampUtc">The snapshot instant (UTC) for this view.</param>
/// <param name="FreshnessState">The bounded-staleness freshness state for this view.</param>
/// <param name="DetailLinkState">The safe detail-link state code (available/request-access/open-detail-disabled).</param>
/// <param name="DisabledDetailReasonCodes">Stable reason codes when the detail link is not openable.</param>
/// <param name="LagIndicator">A safe coarse lag indicator for the audit-projection-lag view, else <see langword="null"/>.</param>
public sealed record OperationalDashboardView(
    DashboardObservabilityView View,
    ChatBotHealthStatus Health,
    int? Depth,
    int OldestItemAgeSeconds,
    string OwnerRole,
    DateTimeOffset FreshnessTimestampUtc,
    ChatBotFreshnessState FreshnessState,
    string DetailLinkState,
    IReadOnlyList<string> DisabledDetailReasonCodes,
    string? LagIndicator = null);

/// <summary>
/// Finite-token validator for the operational-dashboard read query and its overview result. It enforces safe
/// tokens, bounded aggregation, defined status/freshness enums, full FR67 view coverage with no duplicates, and
/// UTC freshness timestamps. It carries no business logic and never inspects restricted detail.
/// </summary>
public static class OperationalDashboardContractValidator
{
    public const int DefaultAggregationLimit = 100;
    public const int MaxAggregationLimit = 1000;

    /// <summary>The detail-link state shown when per-item detail is openable through the authorized path.</summary>
    public const string DetailAvailable = "available";

    /// <summary>The detail-link state shown when the caller may escalate / request access.</summary>
    public const string DetailRequestAccess = "request-access";

    /// <summary>The detail-link state shown when the detail link is disabled with a safe reason.</summary>
    public const string DetailDisabled = "open-detail-disabled";

    public static IReadOnlyList<string> Validate(GetOperationalDashboard query)
    {
        ArgumentNullException.ThrowIfNull(query);

        List<string> errors = [];
        if (!Enum.IsDefined(query.ScopeUsed))
        {
            errors.Add("scope_invalid");
        }

        if (!IsRequiredSafeToken(query.CorrelationId))
        {
            errors.Add("correlation_id_invalid");
        }

        if (query.AggregationLimit is < 1 or > MaxAggregationLimit)
        {
            errors.Add("aggregation_limit_invalid");
        }

        return errors;
    }

    public static IReadOnlyList<string> Validate(OperationalDashboardOverview overview)
    {
        ArgumentNullException.ThrowIfNull(overview);

        List<string> errors = [];
        if (!IsRequiredSafeToken(overview.CorrelationId))
        {
            errors.Add("correlation_id_invalid");
        }

        if (!IsRequiredSafeToken(overview.SchemaVersion))
        {
            errors.Add("schema_version_invalid");
        }

        if (!Enum.IsDefined(overview.FreshnessState))
        {
            errors.Add("freshness_state_invalid");
        }

        if (overview.FreshnessTimestampUtc.Offset != TimeSpan.Zero)
        {
            errors.Add("freshness_not_utc");
        }

        HashSet<DashboardObservabilityView> seen = [];
        foreach (OperationalDashboardView view in overview.Views ?? [])
        {
            ValidateView(view, errors);
            if (!seen.Add(view.View))
            {
                errors.Add("view_duplicate");
            }
        }

        foreach (DashboardObservabilityView required in DashboardObservabilityViews.All)
        {
            if (!seen.Contains(required))
            {
                errors.Add("view_missing");
            }
        }

        return errors;
    }

    public static bool IsValid(GetOperationalDashboard query)
        => Validate(query).Count == 0;

    public static bool IsValid(OperationalDashboardOverview overview)
        => Validate(overview).Count == 0;

    public static bool IsRequiredSafeToken(string? value)
        => !string.IsNullOrWhiteSpace(value) && IsSafeToken(value);

    public static bool IsSafeToken(string? value)
        => value is null ||
            value.Length <= 200 &&
            !ContainsSensitiveMarker(value) &&
            value.All(static character => char.IsAsciiLetterOrDigit(character) || character is '.' or '-' or '_' or ':' or '@' or '|');

    private static void ValidateView(OperationalDashboardView view, List<string> errors)
    {
        if (!Enum.IsDefined(view.View))
        {
            errors.Add("view_invalid");
        }

        if (!Enum.IsDefined(view.Health))
        {
            errors.Add("health_invalid");
        }

        if (!Enum.IsDefined(view.FreshnessState))
        {
            errors.Add("freshness_state_invalid");
        }

        if (view.Depth is < 0)
        {
            errors.Add("depth_invalid");
        }

        if (view.OldestItemAgeSeconds < 0)
        {
            errors.Add("age_invalid");
        }

        if (!IsRequiredSafeToken(view.OwnerRole))
        {
            errors.Add("owner_role_invalid");
        }

        if (view.DetailLinkState is not (DetailAvailable or DetailRequestAccess or DetailDisabled))
        {
            errors.Add("detail_link_state_invalid");
        }

        if (view.FreshnessTimestampUtc.Offset != TimeSpan.Zero)
        {
            errors.Add("freshness_not_utc");
        }

        if (!IsSafeToken(view.LagIndicator))
        {
            errors.Add("lag_indicator_invalid");
        }

        foreach (string reasonCode in view.DisabledDetailReasonCodes ?? [])
        {
            if (!IsRequiredSafeToken(reasonCode))
            {
                errors.Add("disabled_detail_reason_invalid");
            }
        }
    }

    private static bool ContainsSensitiveMarker(string value)
    {
        string[] markers =
        [
            "secret",
            "password",
            "bearer",
            "token",
            "exception",
            ".txt",
            ".json",
            ".xml",
        ];

        return markers.Any(marker => value.Contains(marker, StringComparison.OrdinalIgnoreCase));
    }
}
