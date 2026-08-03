using System.Collections.Concurrent;

using Hexalith.ChatBot.Server.Audit;

namespace Hexalith.ChatBot.IntegrationTests.Recovery;

/// <summary>Diagnostic decorator that preserves live scoped-outage failures after the coordinator fails closed.</summary>
internal sealed class CapturingScopedOutageInjectionDriver(IScopedOutageInjectionDriver inner) : IScopedOutageInjectionDriver
{
    private readonly ConcurrentDictionary<string, Exception> _failures = new(StringComparer.Ordinal);

    /// <summary>Gets the dependency failures captured during the current sweep.</summary>
    public IReadOnlyDictionary<string, Exception> Failures => _failures;

    /// <inheritdoc />
    public async ValueTask<ScopedOutageDegradationMeasurement> InjectAndMeasureAsync(
        string dependency,
        string testTenantRef,
        string correlationId,
        CancellationToken cancellationToken)
    {
        try
        {
            return await inner
                .InjectAndMeasureAsync(dependency, testTenantRef, correlationId, cancellationToken)
                .ConfigureAwait(false);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            _failures[dependency] = new InvalidOperationException($"Scoped-outage scenario '{dependency}' failed.", exception);
            throw;
        }
    }
}
