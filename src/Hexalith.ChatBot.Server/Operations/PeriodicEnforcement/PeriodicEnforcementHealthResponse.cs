namespace Hexalith.ChatBot.Server.Operations.PeriodicEnforcement;

/// <summary>
/// The anonymous liveness payload for the scheduler. It deliberately carries no M2 state and never returns 503.
/// </summary>
internal sealed record PeriodicEnforcementHealthResponse(
    bool IsRunning,
    DateTimeOffset? LastStartedAtUtc,
    DateTimeOffset? LastSucceededAtUtc,
    DateTimeOffset? LastFailedAtUtc,
    TimeSpan? LastDuration,
    long SkippedOverlapCount,
    string? LastCorrelationId,
    PeriodicEnforcementRunbookEvidence? LastRunbookSweep)
{
    public static PeriodicEnforcementHealthResponse From(PeriodicEnforcementRunStatus status)
    {
        ArgumentNullException.ThrowIfNull(status);
        return new PeriodicEnforcementHealthResponse(
            status.IsRunning,
            status.LastStartedAtUtc,
            status.LastSucceededAtUtc,
            status.LastFailedAtUtc,
            status.LastDuration,
            status.SkippedOverlapCount,
            status.LastCorrelationId,
            status.LastRunbookSweep);
    }
}
