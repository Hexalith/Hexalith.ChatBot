using Hexalith.EventStore.ServiceDefaults.Authentication;
using Microsoft.Extensions.Logging;

namespace Hexalith.ChatBot.Server.Tests.Adapters.OwnerServices;

/// <summary>Captures rendered authentication diagnostics for secret-redaction assertions.</summary>
internal sealed class OwnerAuthenticationTestLogger : ILogger<JwtWorkloadAssertionIssuer>
{
    /// <summary>Gets the captured diagnostic messages.</summary>
    public List<string> Messages { get; } = [];

    /// <inheritdoc />
    public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

    /// <inheritdoc />
    public bool IsEnabled(LogLevel logLevel) => true;

    /// <inheritdoc />
    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception,
        Func<TState, Exception?, string> formatter) => Messages.Add(formatter(state, exception));
}
