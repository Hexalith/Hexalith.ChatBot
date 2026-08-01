namespace Hexalith.ChatBot.Server.Audit;

/// <summary>
/// Fail-safe product default. Live outage injection is available only when the opted-in Tier-3 recovery harness
/// constructs its separate driver with closed sandbox operations.
/// </summary>
internal sealed class DeferredScopedOutageInjectionDriver : IScopedOutageInjectionDriver
{
    /// <inheritdoc />
    public ValueTask<ScopedOutageDegradationMeasurement> InjectAndMeasureAsync(
        string dependency,
        string testTenantRef,
        string correlationId,
        CancellationToken cancellationToken)
        => throw new NotSupportedException(
            "scoped-outage validation requires the opted-in Tier-3 recovery harness");
}
