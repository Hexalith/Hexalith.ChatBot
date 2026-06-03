namespace Hexalith.ChatBot.Server.Audit;

/// <summary>
/// The closed set of continuity-drill verdicts (Story 9.11, AC1/AC4). The pure <see cref="ContinuityDrillEvaluator"/>
/// returns <see cref="Met"/> or <see cref="Missed"/> over available measurements; the coordinator returns
/// <see cref="Unmeasurable"/> for a drill that could not complete (fail-safe — never a fabricated <see cref="Met"/>).
/// <para>
/// The three verdicts are kept distinct (mirroring <c>ReplayIsolationStatus</c> Clean/Breach/Unknown): a
/// <see cref="Missed"/> drill is <b>honest evidence that the A10 [ASSUMPTION] target needs recalibration</b> (NOT
/// stop-ship), while an <see cref="Unmeasurable"/> drill is the fail-safe breach (no evidence produced). Never collapse
/// <see cref="Missed"/> or <see cref="Unmeasurable"/> into <see cref="Met"/>.
/// </para>
/// <para>
/// The literals deliberately avoid the legacy-lifecycle tokens so the scaffold-architecture guard does not flag them.
/// </para>
/// </summary>
internal static class ContinuityDrillVerdicts
{
    /// <summary>Both measured durations are within target and no data loss was detected.</summary>
    public const string Met = "met";

    /// <summary>A measured target was exceeded or data loss was detected — a recorded deviation flagging recalibration.</summary>
    public const string Missed = "missed";

    /// <summary>The drill could not complete (runner threw, recovery never finished, durations unavailable) — the fail-safe breach.</summary>
    public const string Unmeasurable = "unmeasurable";

    /// <summary>The closed set of all verdict tokens.</summary>
    public static readonly IReadOnlySet<string> All =
        new HashSet<string>(StringComparer.Ordinal) { Met, Missed, Unmeasurable };

    /// <summary>Returns <see langword="true"/> only for a known verdict token.</summary>
    public static bool Contains(string? verdict)
        => verdict is not null && All.Contains(verdict);
}
