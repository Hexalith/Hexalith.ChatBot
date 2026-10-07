using Hexalith.ChatBot.Server.Authorization;
using Microsoft.Extensions.Logging;

namespace Hexalith.ChatBot.Server.Tests;

/// <summary>Captures actual formatted metadata diagnostics for leakage assertions.</summary>
internal sealed class TrustedAuthorityCaptureLogger : ILogger<ChatBotRequestAuthorizer>
{
    /// <summary>The formatted messages actually emitted by the authorizer.</summary>
    public List<string> Messages { get; } = [];
    /// <inheritdoc/>
    public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;
    /// <inheritdoc/>
    public bool IsEnabled(LogLevel logLevel) => true;
    /// <inheritdoc/>
    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
        => Messages.Add(formatter(state, exception));
}
