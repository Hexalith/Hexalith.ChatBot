namespace Hexalith.ChatBot.Server.Audit;

/// <summary>
/// The structured result of a projection-rebuild validation sweep across every baseline dataset (Story 9.12, AC4). A
/// CI/release gate asserts against the dimension it cares about — e.g. <c>Divergent == 0 &amp;&amp; Unmeasurable == 0</c>
/// ⇒ the rebuilds are deterministic and produced evidence — while a <see cref="DurationExceeded"/> is a recovery-time
/// recalibration signal kept distinct from a determinism failure. Mirrors <see cref="ContinuityDrillOutcome"/>.
/// </summary>
internal sealed record ProjectionRebuildOutcome(
    int TenantsValidated,
    int Equivalent,
    int Divergent,
    int DurationExceeded,
    int Unmeasurable,
    int Alerted);
