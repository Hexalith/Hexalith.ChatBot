namespace Hexalith.ChatBot.Server.Audit;

/// <summary>
/// The closed set of scoped-outage degradation verdicts (Story 9.13, AC1/AC2/AC4). The pure
/// <see cref="ScopedOutageDegradationEvaluator"/> returns <see cref="Contained"/> or <see cref="Breached"/> over the
/// measured assertions; the coordinator returns <see cref="Unmeasurable"/> for a validation that could not complete
/// (fail-safe — never a fabricated <see cref="Contained"/>). Mirrors <see cref="ContinuityDrillVerdicts"/> /
/// <see cref="ProjectionRebuildVerdicts"/>.
/// <para>
/// The three verdicts are kept distinct: a <see cref="Breached"/> validation is the <b>serious</b> NFR58/NFR59
/// isolation/scope/recovery breach (stop-ship-style, like a 9.4/9.5 isolation breach — a gate asserts
/// <c>Breached == 0</c>); an <see cref="Unmeasurable"/> validation is the fail-safe breach (no evidence produced). A late
/// scope recording is folded into the report's <c>ScopeRecordedWithinTarget</c> boolean (a monitoring-latency miss),
/// <b>not</b> the verdict. Never collapse <see cref="Breached"/> or <see cref="Unmeasurable"/> into <see cref="Contained"/>.
/// </para>
/// <para>
/// The literals deliberately avoid the legacy-lifecycle tokens so the scaffold-architecture guard does not flag them.
/// </para>
/// </summary>
internal static class ScopedOutageDegradationVerdicts
{
    /// <summary>The degradation stayed within the expected narrowest scope and all three NFR59 isolation assertions plus the recovery checks held.</summary>
    public const string Contained = "contained";

    /// <summary>A serious NFR58/NFR59 isolation/scope/recovery assertion failed — the stop-ship-style breach.</summary>
    public const string Breached = "breached";

    /// <summary>The validation could not complete (driver threw, outage exercise never finished, assertions unavailable) — the fail-safe breach.</summary>
    public const string Unmeasurable = "unmeasurable";

    /// <summary>The closed set of all verdict tokens.</summary>
    public static readonly IReadOnlySet<string> All =
        new HashSet<string>(StringComparer.Ordinal) { Contained, Breached, Unmeasurable };

    /// <summary>Returns <see langword="true"/> only for a known verdict token.</summary>
    public static bool Contains(string? verdict)
        => verdict is not null && All.Contains(verdict);
}
