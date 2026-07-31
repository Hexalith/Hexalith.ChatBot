namespace Hexalith.ChatBot.Server.Operations.PeriodicEnforcement;

/// <summary>
/// Per-sweep M2 status. <see cref="LastBreaches"/>, <see cref="LastCoverage"/> and
/// <see cref="LastSuccessCorrelationId"/> describe the last sweep that completed; the
/// <see cref="LastRanAtUtc"/>/<see cref="LastRunCorrelationId"/> pair describes the latest attempt.
/// <see cref="LastAttemptCompletedSuccessfully"/> prevents a newer failure or in-progress attempt from making an
/// older clean result releasable.
/// </summary>
internal sealed record PeriodicEnforcementM2SweepStatus(
    DateTimeOffset? LastRanAtUtc,
    string? LastRunCorrelationId,
    DateTimeOffset? LastSucceededAtUtc,
    string? LastSuccessCorrelationId,
    int? LastBreaches,
    int? LastCoverage,
    int? LastPopulation,
    bool LastAttemptCompletedSuccessfully);
