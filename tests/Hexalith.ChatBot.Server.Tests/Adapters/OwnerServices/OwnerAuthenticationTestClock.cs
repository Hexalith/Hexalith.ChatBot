namespace Hexalith.ChatBot.Server.Tests.Adapters.OwnerServices;

internal sealed class OwnerAuthenticationTestClock : TimeProvider
{
    private DateTimeOffset _now = DateTimeOffset.UtcNow;
    public override DateTimeOffset GetUtcNow() => _now;
    public void Advance(TimeSpan duration) => _now += duration;
}
