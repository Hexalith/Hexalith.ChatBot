namespace Hexalith.ChatBot.Server.Audit;

/// <summary>
/// The closed set of NFR59 dependency-outage scenarios the scoped-outage degradation validation sweeps (Story 9.13,
/// AC1/AC2). The six members are exactly the NFR59 dependencies and the sweep
/// (<see cref="ScopedOutageDegradationValidationCoordinator.RunAllScenariosAsync"/>) runs <b>every</b> one. Mirrors the
/// closed-set discipline of <see cref="ContinuityDrillScenarios"/>: a fixed, bounded token vocabulary with an
/// <see cref="All"/> set and a null-safe <see cref="Contains"/> membership check, so an unknown/unsafe dependency token
/// biases to <c>unmeasurable</c> (fail-safe), never a fabricated <c>contained</c>.
/// <para>
/// <b><see cref="Graph"/> covers both degraded Graph access and the expired-subscription lapse.</b> Per architecture the
/// M365/Graph dependency is the mailbox boundary (degraded per-mailbox, never tenant-wide), so the <see cref="Graph"/>
/// scenario validates NFR59's "degraded Graph access" <b>and</b> "expired subscriptions" — both degrade at the mailbox
/// boundary. There is deliberately <b>no</b> seventh <c>subscription-expiry</c> token; <see cref="AiProvider"/>,
/// <see cref="CommandExecution"/>, <see cref="AuditStore"/>, and <see cref="AttachmentProcessing"/> map 1:1 to NFR59, and
/// <see cref="Identity"/> (Keycloak) is the AC1 sixth dependency.
/// </para>
/// <para>
/// The literals deliberately avoid the legacy-lifecycle tokens (<c>pending</c>/<c>accepted</c>/<c>running</c>/
/// <c>succeeded</c>/<c>cancelled</c>) so the scaffold-architecture guard does not flag them and no allowlist entry is
/// needed.
/// </para>
/// </summary>
internal static class ScopedOutageDependencies
{
    /// <summary>Degraded Microsoft Graph access <b>and</b> the expired-subscription lapse — both degrade at the mailbox boundary (NFR59).</summary>
    public const string Graph = "graph";

    /// <summary>An identity-provider (Keycloak) outage — the AC1 sixth dependency.</summary>
    public const string Identity = "identity";

    /// <summary>An AI-provider outage (NFR59).</summary>
    public const string AiProvider = "ai-provider";

    /// <summary>A command-execution failure (NFR59).</summary>
    public const string CommandExecution = "command-execution";

    /// <summary>An audit-store unavailability (NFR59).</summary>
    public const string AuditStore = "audit-store";

    /// <summary>A partial attachment-processing failure (NFR59).</summary>
    public const string AttachmentProcessing = "attachment-processing";

    /// <summary>
    /// The deterministic order in which the live sweep must run the closed set. This is a CONTRACT, not a
    /// convenience: <see cref="Identity"/> is intentionally last because stopping the topology's security root can
    /// invalidate dependent endpoint leases even after Keycloak itself is healthy, so no provider-boundary scenario
    /// may rely on those endpoints after the identity drill. It is an ordered list because
    /// <see cref="HashSet{T}"/> enumeration order is an unspecified implementation detail and would silently
    /// reorder the destructive sweep on any future insertion.
    /// </summary>
    public static readonly IReadOnlyList<string> SweepOrder =
    [
        Graph,
        AiProvider,
        CommandExecution,
        AuditStore,
        AttachmentProcessing,
        Identity,
    ];

    /// <summary>The closed set of all NFR59-required dependency-outage scenarios; the sweep runs every member.</summary>
    /// <exception cref="InvalidOperationException">A duplicate entry in <see cref="SweepOrder"/> would silently
    /// dedupe into this set, so the sweep would inject one outage twice while the release gate saw one manifest too
    /// many and reported <c>incomplete_scenario_set</c> on every run thereafter.</exception>
    public static readonly IReadOnlySet<string> All = BuildClosedSet();

    /// <summary>Returns <see langword="true"/> only for a known dependency token; any other value is unknown (fail-safe).</summary>
    public static bool Contains(string? dependency)
        => dependency is not null && All.Contains(dependency);

    private static IReadOnlySet<string> BuildClosedSet()
    {
        HashSet<string> closed = new HashSet<string>(SweepOrder, StringComparer.Ordinal);
        return closed.Count == SweepOrder.Count
            ? closed
            : throw new InvalidOperationException($"{nameof(SweepOrder)} must not contain duplicate dependency tokens.");
    }
}
