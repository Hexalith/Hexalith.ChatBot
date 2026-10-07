using Hexalith.ChatBot.Server.Audit;

namespace Hexalith.ChatBot.Tests.TrustedAuthority;

/// <summary>A deterministic synthetic evidence clock.</summary>
internal sealed class TrustedAuthorityClock : ISystemClock
{
    /// <summary>The test's current UTC instant.</summary>
    public DateTimeOffset UtcNow { get; set; } = new(2026, 10, 7, 10, 0, 0, TimeSpan.Zero);
}
