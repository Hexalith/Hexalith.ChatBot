namespace Hexalith.ChatBot.Server.Audit;

/// <summary>
/// The structured result of a continuity-drill sweep across every <see cref="ContinuityDrillScenarios"/> scenario
/// (Story 9.11, AC4). A CI/release gate asserts against the dimension it cares about — e.g.
/// <c>Unmeasurable == 0</c> ⇒ the drills ran and produced evidence (the fail-safe breach is an unmeasurable drill),
/// distinct from <c>Missed == 0</c> ⇒ every target met. Mirrors <c>DerivedStoreIsolationProbeOutcome</c>.
/// </summary>
internal sealed record ContinuityDrillOutcome(int ScenariosRun, int Met, int Missed, int Unmeasurable, int Alerted);
