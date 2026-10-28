namespace Hexalith.ChatBot.IntegrationTests.Recovery;

/// <summary>Advances a monotonic test clock on each timestamp observation without wall-clock waits.</summary>
/// <param name="step">The interval between timestamp observations.</param>
internal sealed class AdvancingRecoveryTimeProvider(TimeSpan step) : TimeProvider
{
    private long _timestamp = -step.Ticks;

    /// <summary>Gets the last timestamp as elapsed test time without advancing it.</summary>
    internal TimeSpan ObservedElapsed => TimeSpan.FromTicks(Volatile.Read(ref _timestamp));

    /// <inheritdoc />
    public override long TimestampFrequency => TimeSpan.TicksPerSecond;

    /// <inheritdoc />
    public override long GetTimestamp() => Interlocked.Add(ref _timestamp, step.Ticks);
}
