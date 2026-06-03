using Hexalith.ChatBot.Contracts.Enums;

namespace Hexalith.ChatBot.Contracts.Queries;

/// <summary>
/// Bounded-staleness classification for operational-dashboard freshness (NFR6 / NFR48). A snapshot is
/// <see cref="ChatBotFreshnessState.Fresh"/> within the ordinary staleness window (MVP default 5 minutes),
/// <see cref="ChatBotFreshnessState.Stale"/> while still inside the trust window (visually flagged, never an
/// error), and <see cref="ChatBotFreshnessState.Expired"/> once past it. Inputs are server-side UTC instants.
/// </summary>
public static class OperationalDashboardFreshnessPolicy
{
    /// <summary>The MVP default ordinary staleness window for policy/health changes (NFR6): 5 minutes.</summary>
    public static readonly TimeSpan DefaultStalenessWindow = TimeSpan.FromMinutes(5);

    /// <summary>The default expiry window past which a snapshot is no longer trustworthy: 15 minutes.</summary>
    public static readonly TimeSpan DefaultExpiryWindow = TimeSpan.FromMinutes(15);

    /// <summary>Classifies a snapshot using the MVP default staleness and expiry windows.</summary>
    /// <param name="snapshotUtc">The snapshot instant (UTC).</param>
    /// <param name="nowUtc">The current instant (UTC).</param>
    /// <returns>The freshness state.</returns>
    public static ChatBotFreshnessState Classify(DateTimeOffset snapshotUtc, DateTimeOffset nowUtc)
        => Classify(snapshotUtc, nowUtc, DefaultStalenessWindow, DefaultExpiryWindow);

    /// <summary>Classifies a snapshot using explicit staleness and expiry windows.</summary>
    /// <param name="snapshotUtc">The snapshot instant (UTC).</param>
    /// <param name="nowUtc">The current instant (UTC).</param>
    /// <param name="stalenessWindow">The fresh-to-stale boundary.</param>
    /// <param name="expiryWindow">The stale-to-expired boundary (must be at least the staleness window).</param>
    /// <returns>The freshness state.</returns>
    public static ChatBotFreshnessState Classify(
        DateTimeOffset snapshotUtc,
        DateTimeOffset nowUtc,
        TimeSpan stalenessWindow,
        TimeSpan expiryWindow)
    {
        TimeSpan effectiveExpiry = expiryWindow < stalenessWindow ? stalenessWindow : expiryWindow;
        TimeSpan age = nowUtc - snapshotUtc;

        if (age <= stalenessWindow)
        {
            // Within the ordinary window (including a future-skewed snapshot) the reference is fresh.
            return ChatBotFreshnessState.Fresh;
        }

        return age <= effectiveExpiry
            ? ChatBotFreshnessState.Stale
            : ChatBotFreshnessState.Expired;
    }
}
