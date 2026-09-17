namespace Hexalith.ChatBot.Server.Audit;

/// <summary>
/// The measured result the <see cref="IProjectionRebuildDriver"/> seam returns to the coordinator: the wall-clock bounds,
/// the measured rebuild duration, the pre-rebuild + rebuilt structural snapshots, and the two stamped projection schema
/// versions. The pure <see cref="ProjectionRebuildEquivalenceEvaluator"/> folds the snapshots + schema versions into a
/// verdict; the coordinator compares <see cref="MeasuredDuration"/> against <see cref="RecoveryTargets.MaxRto"/>.
/// </summary>
internal sealed record ProjectionRebuildMeasurement(
    DateTimeOffset StartedAtUtc,
    DateTimeOffset EndedAtUtc,
    TimeSpan MeasuredDuration,
    IReadOnlyList<ProjectionResourceDigest> PreRebuildSnapshot,
    IReadOnlyList<ProjectionResourceDigest> RebuiltSnapshot,
    string PreRebuildSchemaVersion,
    string RebuiltSchemaVersion,
    RecoveryValidationExecutionAssertions? ExecutionAssertions = null,
    int SourceResourceCount = 0,
    int GovernedResourceCount = 0,
    int WormRecordCount = 0,
    int WormOperationCount = 0);
