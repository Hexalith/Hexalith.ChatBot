using System.Collections.Concurrent;
using System.Net;
using System.Net.Http.Json;

using Hexalith.ChatBot.Server.Projections;

using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

using Shouldly;

namespace Hexalith.ChatBot.Server.Tests.Projections;

/// <summary>
/// Behavioral guards for the dead-letter drain. The seven projection subscriptions route poison messages to the
/// configured dead-letter topic; before this endpoint existed nothing subscribed to it, so failures moved from
/// "retried" to "silently accumulating in Redis".
/// </summary>
public sealed class ChatBotDeadLetterProjectionEndpointsTests
{
    private const string MessageId = "01ARZ3NDEKTSV4RRFFQ69G5FAY";
    private const string CorrelationId = "01ARZ3NDEKTSV4RRFFQ69G5FAW";

    /// <summary>A value that must never reach a log sink, planted in the payload of the poison envelope.</summary>
    private const string SensitivePayloadMarker = "restricted-evidence-body-must-never-be-logged";

    /// <summary>The log category the dead-letter drain writes under.</summary>
    private static readonly string DrainCategory = typeof(ChatBotDeadLetterProjectionEndpoints).FullName!;

    [Fact]
    public async Task DeadLetteredMessageShouldBeDrainedWithMetadataOnlyErrorLog()
    {
        RecordingLoggerProvider recorder = new();
        using WebApplicationFactory<Program> factory = new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder => builder.ConfigureServices(services =>
                services.AddSingleton<ILoggerProvider>(recorder)));
        using HttpClient client = factory.CreateClient();

        using HttpResponseMessage response = await client
            .PostAsJsonAsync(
                ChatBotDeadLetterProjectionEndpoints.DeadLetterDrainRoute,
                PoisonEnvelope(),
                TestContext.Current.CancellationToken)
            .ConfigureAwait(true);

        // 200 drains the message: a non-2xx would make DAPR redeliver the poison message forever.
        response.StatusCode.ShouldBe(HttpStatusCode.OK);

        string deadLetterLog = recorder.Entries
            .Where(static entry => entry.Level == LogLevel.Error
                && string.Equals(entry.Category, DrainCategory, StringComparison.Ordinal))
            .Select(static entry => entry.Message)
            .ShouldHaveSingleItem("The dead-letter drain must log exactly one Error per dead-lettered message.");

        // Metadata identity: pub/sub component, dead-letter topic, message and correlation identity.
        deadLetterLog.ShouldContain(ChatBotProjectionSubscriptionDefaults.PubSubName);
        deadLetterLog.ShouldContain(ChatBotProjectionSubscriptionDefaults.DeadLetterTopic);
        deadLetterLog.ShouldContain(MessageId);
        deadLetterLog.ShouldContain(CorrelationId);
    }

    [Fact]
    public async Task DeadLetterDrainMustNeverLogTheMessageBody()
    {
        RecordingLoggerProvider recorder = new();
        using WebApplicationFactory<Program> factory = new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder => builder.ConfigureServices(services =>
                services.AddSingleton<ILoggerProvider>(recorder)));
        using HttpClient client = factory.CreateClient();

        using HttpResponseMessage response = await client
            .PostAsJsonAsync(
                ChatBotDeadLetterProjectionEndpoints.DeadLetterDrainRoute,
                PoisonEnvelope(),
                TestContext.Current.CancellationToken)
            .ConfigureAwait(true);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);

        // Epic 1 redaction floor: no field of the dead-lettered envelope other than its identity may reach a log
        // sink, at any level, under any category. The payload marker is planted only in this request's body, so any
        // occurrence anywhere in the captured log is a leak from this path.
        foreach (RecordedLogEntry entry in recorder.Entries)
        {
            entry.Message.ShouldNotContain(SensitivePayloadMarker, Case.Insensitive);
        }

        // The drain's own entry additionally carries no tenant, domain, aggregate id, event type, or sequence
        // number -- only the pub/sub, topic and identity the metadata-only contract allows.
        string deadLetterLog = recorder.Entries
            .Where(static entry => string.Equals(entry.Category, DrainCategory, StringComparison.Ordinal))
            .Select(static entry => entry.Message)
            .ShouldHaveSingleItem();
        foreach (string forbidden in new[] { "tenant-alpha", "01ARZ3NDEKTSV4RRFFQ69G5FAZ", "GovernedNoteRecorded" })
        {
            deadLetterLog.ShouldNotContain(forbidden, Case.Insensitive);
        }
    }

    [Fact]
    public async Task MalformedDeadLetteredIdentityShouldStillDrainWithoutLeakingTheRawValue()
    {
        // A dead-lettered message is by definition one the projection path rejected, so its own fields cannot be
        // assumed well-formed. An unsafe identity must be reduced, never forwarded verbatim into a log sink.
        RecordingLoggerProvider recorder = new();
        using WebApplicationFactory<Program> factory = new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder => builder.ConfigureServices(services =>
                services.AddSingleton<ILoggerProvider>(recorder)));
        using HttpClient client = factory.CreateClient();

        using HttpResponseMessage response = await client
            .PostAsJsonAsync(
                ChatBotDeadLetterProjectionEndpoints.DeadLetterDrainRoute,
                new
                {
                    messageId = $"password={SensitivePayloadMarker}",
                    correlationId = (string?)null,
                },
                TestContext.Current.CancellationToken)
            .ConfigureAwait(true);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);

        string deadLetterLog = recorder.Entries
            .Where(static entry => entry.Level == LogLevel.Error
                && string.Equals(entry.Category, DrainCategory, StringComparison.Ordinal))
            .Select(static entry => entry.Message)
            .ShouldHaveSingleItem();
        deadLetterLog.ShouldNotContain(SensitivePayloadMarker, Case.Insensitive);
        deadLetterLog.ShouldContain("unknown");
    }

    private static object PoisonEnvelope()
        => new
        {
            tenantId = "tenant-alpha",
            domain = "chatbot",
            aggregateId = "01ARZ3NDEKTSV4RRFFQ69G5FAZ",
            eventTypeName = "Hexalith.ChatBot.Contracts.Events.GovernedNoteRecorded",
            sequenceNumber = 1L,
            correlationId = CorrelationId,
            messageId = MessageId,
            timestamp = new DateTimeOffset(2026, 9, 14, 8, 0, 0, TimeSpan.Zero),
            payload = SensitivePayloadMarker,
        };

    private sealed record RecordedLogEntry(LogLevel Level, string Category, string Message);

    private sealed class RecordingLoggerProvider : ILoggerProvider
    {
        private readonly ConcurrentQueue<RecordedLogEntry> _entries = new();

        public IReadOnlyCollection<RecordedLogEntry> Entries => [.. _entries];

        public ILogger CreateLogger(string categoryName) => new RecordingLogger(categoryName, _entries);

        public void Dispose()
        {
        }

        private sealed class RecordingLogger(string category, ConcurrentQueue<RecordedLogEntry> entries) : ILogger
        {
            public IDisposable? BeginScope<TState>(TState state)
                where TState : notnull => null;

            public bool IsEnabled(LogLevel logLevel) => true;

            public void Log<TState>(
                LogLevel logLevel,
                EventId eventId,
                TState state,
                Exception? exception,
                Func<TState, Exception?, string> formatter)
            {
                ArgumentNullException.ThrowIfNull(formatter);
                entries.Enqueue(new RecordedLogEntry(logLevel, category, formatter(state, exception)));
            }
        }
    }
}
