using System.Collections.Concurrent;
using System.Diagnostics;
using System.Text.Json;

using Microsoft.Extensions.Logging;

namespace Hexalith.ChatBot.Conformance.Tests;

/// <summary>Uses the existing endpoint-test ILoggerProvider and ActivityListener capture patterns for real host output.</summary>
internal sealed class TrustedAuthorityOutputCapture : ILoggerProvider
{
    private readonly ConcurrentQueue<string> _logs = new();
    private readonly ConcurrentQueue<string> _traces = new();
    private readonly ActivityListener _listener;

    /// <summary>Starts capture before the protected host is created.</summary>
    public TrustedAuthorityOutputCapture()
    {
        _listener = new ActivityListener
        {
            ShouldListenTo = static _ => true,
            Sample = static (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllDataAndRecorded,
            SampleUsingParentId = static (ref ActivityCreationOptions<string> _) => ActivitySamplingResult.AllDataAndRecorded,
            ActivityStopped = activity => _traces.Enqueue(JsonSerializer.Serialize(new
            {
                activity.Source.Name,
                activity.DisplayName,
                activity.StatusDescription,
                Tags = activity.TagObjects.ToArray(),
                Baggage = activity.Baggage.ToArray(),
                Events = activity.Events.Select(static entry => new { entry.Name, Tags = entry.Tags.ToArray() }).ToArray(),
            })),
        };
        ActivitySource.AddActivityListener(_listener);
    }

    /// <summary>Actual formatted, structured and exception log output.</summary>
    public IReadOnlyCollection<string> Logs => _logs.ToArray();
    /// <summary>Actual completed span tags, events and baggage.</summary>
    public IReadOnlyCollection<string> Traces => _traces.ToArray();
    /// <inheritdoc/>
    public ILogger CreateLogger(string categoryName) => new CaptureLogger(categoryName, _logs);
    /// <inheritdoc/>
    public void Dispose() => _listener.Dispose();

    private sealed class CaptureLogger(string category, ConcurrentQueue<string> entries) : ILogger
    {
        public IDisposable? BeginScope<TState>(TState state)
            where TState : notnull
        {
            entries.Enqueue(JsonSerializer.Serialize(new { category, Scope = state.ToString() }));
            return null;
        }

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
        {
            ArgumentNullException.ThrowIfNull(formatter);
            entries.Enqueue(JsonSerializer.Serialize(new { category, Level = logLevel.ToString(), Message = formatter(state, exception), State = state is null ? null : state.ToString(), Exception = exception?.ToString() }));
        }
    }
}
