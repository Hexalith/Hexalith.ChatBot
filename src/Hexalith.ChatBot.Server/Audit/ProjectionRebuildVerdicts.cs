namespace Hexalith.ChatBot.Server.Audit;

/// <summary>
/// The closed set of projection-rebuild verdicts (Story 9.12, AC2/AC4). The pure
/// <see cref="ProjectionRebuildEquivalenceEvaluator"/> returns <see cref="Equivalent"/> or <see cref="Divergent"/> over
/// available structural snapshots; the coordinator returns <see cref="Unmeasurable"/> for a validation that could not
/// complete (fail-safe — never a fabricated <see cref="Equivalent"/>). Mirrors <see cref="ContinuityDrillVerdicts"/>
/// exactly.
/// <para>
/// The three verdicts are kept distinct (mirroring <c>ReplayIsolationStatus</c> Clean/Breach/Unknown): a
/// <see cref="Divergent"/> rebuild is the <b>serious determinism breach</b> — it makes evidence snapshots / approval
/// records non-reproducible (NFR49a, architecture invariant #11) — while an <see cref="Unmeasurable"/> validation is the
/// fail-safe breach (no evidence produced). Never collapse <see cref="Divergent"/> or <see cref="Unmeasurable"/> into
/// <see cref="Equivalent"/>.
/// </para>
/// <para>
/// The literals deliberately avoid the legacy-lifecycle tokens (<c>pending</c>/<c>accepted</c>/<c>running</c>/
/// <c>succeeded</c>/<c>cancelled</c>) so the scaffold-architecture guard does not flag them and no allowlist entry is
/// needed.
/// </para>
/// </summary>
internal static class ProjectionRebuildVerdicts
{
    /// <summary>The rebuilt projection is deterministically equivalent to the pre-rebuild projection (same schema version, key set, and structural tokens).</summary>
    public const string Equivalent = "equivalent";

    /// <summary>The rebuilt projection differs from the pre-rebuild projection — a non-deterministic rebuild (the serious NFR49a/invariant-#11 breach).</summary>
    public const string Divergent = "divergent";

    /// <summary>The validation could not complete (driver threw, rebuild never finished, snapshots/durations unavailable) — the fail-safe breach.</summary>
    public const string Unmeasurable = "unmeasurable";

    /// <summary>The closed set of all verdict tokens.</summary>
    public static readonly IReadOnlySet<string> All =
        new HashSet<string>(StringComparer.Ordinal) { Equivalent, Divergent, Unmeasurable };

    /// <summary>Returns <see langword="true"/> only for a known verdict token.</summary>
    public static bool Contains(string? verdict)
        => verdict is not null && All.Contains(verdict);
}
