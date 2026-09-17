using Hexalith.ChatBot.Contracts.Enums;

namespace Hexalith.ChatBot.Contracts.Queries;

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
/// <param name="AffectedScope">The NFR42 affected-scope element for a degraded/failed view, in <c>{scopeKind}:{token}</c> form; <see langword="null"/> for healthy/unknown views.</param>
/// <param name="ScopeKind">The wire token of the resolved <see cref="Enums.DependencyScopeKind"/> for a degraded/failed view, else <see langword="null"/>.</param>
/// <param name="NextSafeAction">The NFR42 next-safe-action affordance for a degraded/failed view, else <see langword="null"/>.</param>
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
    string? LagIndicator = null,
    string? AffectedScope = null,
    string? ScopeKind = null,
    string? NextSafeAction = null);
