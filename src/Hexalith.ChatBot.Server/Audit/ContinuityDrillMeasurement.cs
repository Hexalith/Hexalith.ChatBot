namespace Hexalith.ChatBot.Server.Audit;

/// <summary>
/// The measured result the <see cref="IContinuityDrillScenarioRunner"/> seam returns from running one recovery scenario:
/// the wall-clock bounds and the measured RPO/RTO plus the data-loss check. The pure
/// <see cref="ContinuityDrillEvaluator"/> folds these into a verdict.
/// </summary>
internal sealed record ContinuityDrillMeasurement(
    DateTimeOffset StartedAtUtc,
    DateTimeOffset EndedAtUtc,
    TimeSpan MeasuredRpo,
    TimeSpan MeasuredRto,
    bool DataLossDetected,
    RecoveryValidationExecutionAssertions? ExecutionAssertions = null);
