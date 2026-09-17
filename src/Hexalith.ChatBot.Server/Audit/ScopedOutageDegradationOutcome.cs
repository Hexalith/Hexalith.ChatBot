namespace Hexalith.ChatBot.Server.Audit;

/// <summary>
/// The structured result of a scoped-outage degradation validation sweep across every
/// <see cref="ScopedOutageDependencies"/> scenario (Story 9.13, AC4). A CI/release gate asserts against the dimension it
/// cares about — e.g. <c>Breached == 0 &amp;&amp; Unmeasurable == 0</c> ⇒ every dependency outage degraded only its scope
/// and produced evidence — while a <see cref="ScopeRecordingExceeded"/> is a monitoring-latency recalibration signal kept
/// distinct from an isolation breach. Mirrors <see cref="ProjectionRebuildOutcome"/> / <see cref="ContinuityDrillOutcome"/>.
/// </summary>
internal sealed record ScopedOutageDegradationOutcome(
    int ScenariosValidated,
    int Contained,
    int Breached,
    int ScopeRecordingExceeded,
    int Unmeasurable,
    int Alerted);
