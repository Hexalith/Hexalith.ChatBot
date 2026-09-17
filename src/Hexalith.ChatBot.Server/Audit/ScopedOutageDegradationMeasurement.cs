namespace Hexalith.ChatBot.Server.Audit;

/// <summary>
/// The measured result the <see cref="IScopedOutageInjectionDriver"/> seam returns to the coordinator: the expected vs
/// observed degradation scope, the three NFR59 isolation assertions, the NFR17/NFR13 recovery checks, the measured
/// NFR41 detection→scope-recording latency, and the wall-clock bounds. The pure
/// <see cref="ScopedOutageDegradationEvaluator"/> folds the assertions + scopes into a verdict; the coordinator compares
/// <see cref="ScopeRecordingLatency"/> against <see cref="RecoveryTargets.MaxScopeRecordingLatency"/>.
/// <para>
/// <see cref="InflightItemsRecoverable"/> is the <b>positive</b> NFR17 assertion — the evaluator records
/// <c>inflight_not_recoverable</c> when it is <see langword="false"/>; an <see cref="ObservedScope"/> outside the
/// <see cref="ExpectedScope"/> is the NFR58 scope escape.
/// </para>
/// </summary>
internal sealed record ScopedOutageDegradationMeasurement(
    string ExpectedScope,
    string ObservedScope,
    bool CrossTenantLeakageDetected,
    bool UnauthorizedMutationDetected,
    bool SilentDataLossDetected,
    bool InflightItemsRecoverable,
    bool DuplicateSideEffectDetected,
    TimeSpan ScopeRecordingLatency,
    DateTimeOffset StartedAtUtc,
    DateTimeOffset EndedAtUtc,
    RecoveryValidationExecutionAssertions? ExecutionAssertions = null);
