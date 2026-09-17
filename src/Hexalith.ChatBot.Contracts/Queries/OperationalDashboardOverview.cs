using Hexalith.ChatBot.Contracts.Enums;

namespace Hexalith.ChatBot.Contracts.Queries;

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
/// <param name="PublishedSlos">
/// The metadata-only NFR42a published-SLO catalog with each SLO's current coarse error-budget burn (Story 8.3).
/// Rides the same authorized S8 read as the FR67 <see cref="Views"/>; empty (never <see langword="null"/> in
/// canonical results) when no catalog is wired.
/// </param>
public sealed record OperationalDashboardOverview(
    IReadOnlyList<OperationalDashboardView> Views,
    DateTimeOffset FreshnessTimestampUtc,
    ChatBotFreshnessState FreshnessState,
    string SchemaVersion,
    string CorrelationId,
    IReadOnlyList<PublishedSlo>? PublishedSlos = null);
