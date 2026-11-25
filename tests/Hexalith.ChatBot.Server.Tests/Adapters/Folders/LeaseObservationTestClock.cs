namespace Hexalith.ChatBot.Server.Tests.Adapters.Folders;

/// <summary>Signals once the adapter accepts an observation and schedules its next check.</summary>
internal sealed class LeaseObservationTestClock(TimeProvider inner) : TimeProvider
{
    private readonly TaskCompletionSource _nextObservation = new(TaskCreationOptions.RunContinuationsAsynchronously);
    private int _timers;

    /// <summary>Gets the signal that the adapter registered a second timer.</summary>
    public Task NextObservationScheduled => _nextObservation.Task;

    /// <inheritdoc/>
    public override DateTimeOffset GetUtcNow() => inner.GetUtcNow();

    /// <inheritdoc/>
    public override ITimer CreateTimer(TimerCallback callback, object? state, TimeSpan dueTime, TimeSpan period)
    {
        ITimer timer = inner.CreateTimer(callback, state, dueTime, period);
        if (Interlocked.Increment(ref _timers) == 2)
        {
            _nextObservation.TrySetResult();
        }

        return timer;
    }
}
