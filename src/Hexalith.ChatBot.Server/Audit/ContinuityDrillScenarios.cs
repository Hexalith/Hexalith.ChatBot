namespace Hexalith.ChatBot.Server.Audit;

/// <summary>
/// The closed set of continuity-drill scenarios (Story 9.11, NFR56). Both members are required by NFR56 and the drill
/// sweep (<see cref="ContinuityDrillCoordinator.RunAllScenariosAsync"/>) runs <b>both</b>. Mirrors the closed-set
/// discipline of the replay/test-tenant policy: a fixed, bounded token vocabulary with an <see cref="All"/> set and a
/// <see cref="Contains"/> membership check, so an unknown/unsafe scenario token biases to <c>unmeasurable</c>
/// (fail-safe), never a fabricated <c>met</c>.
/// <para>
/// The literals deliberately avoid the legacy-lifecycle tokens (<c>pending</c>/<c>accepted</c>/<c>running</c>/
/// <c>succeeded</c>/<c>cancelled</c>) so the scaffold-architecture guard does not flag them and no allowlist entry is
/// needed.
/// </para>
/// </summary>
internal static class ContinuityDrillScenarios
{
    /// <summary>A simulated EventStore outage: recovery rebuilds the WORM chain / projections from the event log.</summary>
    public const string EventStoreOutage = "eventstore-outage";

    /// <summary>A simulated Microsoft 365 subscription failure: recovery re-establishes the Graph subscription.</summary>
    public const string M365SubscriptionFailure = "m365-subscription-failure";

    /// <summary>The closed set of all NFR56-required drill scenarios; the sweep runs every member.</summary>
    public static readonly IReadOnlySet<string> All =
        new HashSet<string>(StringComparer.Ordinal) { EventStoreOutage, M365SubscriptionFailure };

    /// <summary>Returns <see langword="true"/> only for a known scenario token; any other value is unknown (fail-safe).</summary>
    public static bool Contains(string? scenario)
        => scenario is not null && All.Contains(scenario);
}
