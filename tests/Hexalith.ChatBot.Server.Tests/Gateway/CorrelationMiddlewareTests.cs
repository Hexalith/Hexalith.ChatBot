using System.Diagnostics;
using System.Text;
using System.Text.Json;

using Hexalith.ChatBot.Server.Gateway.Correlation;

using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

using Shouldly;

namespace Hexalith.ChatBot.Server.Tests.Gateway;

public sealed class CorrelationMiddlewareTests
{
    [Fact]
    public async Task CorrelationMiddlewareShouldTagActivityAndScopeOnlyParsedMetadata()
    {
        RecordingLogger<ChatBotCorrelationMiddleware> logger = new();
        bool nextCalled = false;
        ChatBotCorrelationMiddleware middleware = new(
            context =>
            {
                nextCalled = true;
                context.GetCorrelationContext().CorrelationId.ShouldBe("01ARZ3NDEKTSV4RRFFQ69G5FAW");
                context.GetCorrelationContext().TaskId.ShouldBe("01ARZ3NDEKTSV4RRFFQ69G5FAX");
                return Task.CompletedTask;
            },
            logger);

        DefaultHttpContext httpContext = new();
        httpContext.Request.Headers["X-Correlation-Id"] = "01ARZ3NDEKTSV4RRFFQ69G5FAW";
        httpContext.Request.Headers["X-Hexalith-Task-Id"] = "01ARZ3NDEKTSV4RRFFQ69G5FAX";
        httpContext.Request.Body = new MemoryStream(Encoding.UTF8.GetBytes("payload-sentinel secret /tmp/raw"));

        using ActivitySource source = new("Hexalith.ChatBot.Tests");
        using ActivityListener listener = new()
        {
            ShouldListenTo = static _ => true,
            Sample = static (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllDataAndRecorded,
        };
        ActivitySource.AddActivityListener(listener);
        using Activity? activity = source.StartActivity("correlation-test");

        await middleware.InvokeAsync(httpContext).ConfigureAwait(true);

        nextCalled.ShouldBeTrue();
        activity.ShouldNotBeNull();
        activity.Tags.Single(tag => tag.Key == "hexalith.correlation_id").Value.ShouldBe("01ARZ3NDEKTSV4RRFFQ69G5FAW");
        activity.Tags.Single(tag => tag.Key == "hexalith.task_id").Value.ShouldBe("01ARZ3NDEKTSV4RRFFQ69G5FAX");

        string scopes = JsonSerializer.Serialize(logger.Scopes, new JsonSerializerOptions(JsonSerializerDefaults.Web));
        scopes.ShouldContain("01ARZ3NDEKTSV4RRFFQ69G5FAW", Case.Sensitive);
        scopes.ShouldContain("01ARZ3NDEKTSV4RRFFQ69G5FAX", Case.Sensitive);
        scopes.ShouldNotContain("payload-sentinel", Case.Insensitive);
        scopes.ShouldNotContain("secret", Case.Insensitive);
        scopes.ShouldNotContain("/tmp/raw", Case.Insensitive);
    }

    [Fact]
    public async Task CorrelationMiddlewareScopeShouldReflectCommandIdFallbackReplacement()
    {
        RecordingLogger<ChatBotCorrelationMiddleware> logger = new();
        const string commandIdFallback = "01ARZ3NDEKTSV4RRFFQ69G5FAY";
        ChatBotCorrelationMiddleware middleware = new(
            context =>
            {
                // Mimic the /api/v1/commands endpoint resolving with a commandId fallback when the inbound
                // correlation header was missing/invalid: this replaces the generated fallback value.
                _ = context.ResolveCorrelationContext(commandIdFallback);
                return Task.CompletedTask;
            },
            logger);

        // No X-Correlation-Id header => the middleware opens the scope with a generated ULID first.
        DefaultHttpContext httpContext = new();

        await middleware.InvokeAsync(httpContext).ConfigureAwait(true);

        httpContext.GetCorrelationContext().CorrelationId.ShouldBe(commandIdFallback);

        // The live log scope must reflect the effective (commandId-derived) correlationId returned to the caller,
        // not the orphaned generated value, so logs remain traceable by the id in the response body/header.
        string scopes = JsonSerializer.Serialize(logger.Scopes, new JsonSerializerOptions(JsonSerializerDefaults.Web));
        scopes.ShouldContain(commandIdFallback, Case.Sensitive);
    }

    private sealed class RecordingLogger<T> : ILogger<T>
    {
        public List<object> Scopes { get; } = [];

        public IDisposable? BeginScope<TState>(TState state)
            where TState : notnull
        {
            Scopes.Add(state);
            return NullScope.Instance;
        }

        public bool IsEnabled(LogLevel logLevel)
            => true;

        public void Log<TState>(
            LogLevel logLevel,
            EventId eventId,
            TState state,
            Exception? exception,
            Func<TState, Exception?, string> formatter)
        {
        }

        private sealed class NullScope : IDisposable
        {
            public static readonly NullScope Instance = new();

            public void Dispose()
            {
            }
        }
    }
}
