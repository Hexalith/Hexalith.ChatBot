namespace Hexalith.ChatBot.RecoverySandbox;

/// <summary>Observed outcome from one concrete dependency-seam exercise.</summary>
internal sealed record RecoveryDependencyExerciseResult(
    bool FaultObserved,
    DateTimeOffset ObservedAtUtc,
    int EffectCount,
    bool UnauthorizedMutationDetected,
    bool SilentDataLossDetected,
    bool DuplicateSideEffectDetected,
    bool CrossTenantLeakageDetected);
