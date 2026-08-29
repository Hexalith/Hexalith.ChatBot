using System.Net.Http;
using System.Text.Json;

namespace Hexalith.ChatBot.IntegrationTests.Recovery;

/// <summary>Provides infrastructure-free delegates for concrete scoped-outage producer and cleanup regressions.</summary>
internal sealed class ScopedOutageOperationsTestSeam
{
    /// <summary>Gets or initializes the Graph subscription delegate.</summary>
    internal Func<string, string, bool, CancellationToken, HttpMethod?, ValueTask<JsonDocument>>? SendSubscriptionAsync { get; init; }

    /// <summary>Gets or initializes the dependency restoration delegate.</summary>
    internal Func<string, string, CancellationToken, ValueTask<bool>>? RestoreAsync { get; init; }

    /// <summary>Gets or initializes the recovery-token probe.</summary>
    internal Func<CancellationToken, ValueTask<bool>>? TryAcquireRecoveryTokenOnceAsync { get; init; }

    /// <summary>Gets or initializes the identity availability probe.</summary>
    internal Func<CancellationToken, ValueTask<bool>>? IsIdentityAvailableAsync { get; init; }

    /// <summary>Gets or initializes control-token acquisition.</summary>
    internal Func<CancellationToken, ValueTask<string>>? AcquireControlAccessTokenAsync { get; init; }
}
