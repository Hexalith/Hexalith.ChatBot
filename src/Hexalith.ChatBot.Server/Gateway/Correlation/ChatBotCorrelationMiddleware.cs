using System.Collections;
using System.Diagnostics;

namespace Hexalith.ChatBot.Server.Gateway.Correlation;

internal sealed class ChatBotCorrelationMiddleware(RequestDelegate next, ILogger<ChatBotCorrelationMiddleware> logger)
{
    public async Task InvokeAsync(HttpContext httpContext)
    {
        ArgumentNullException.ThrowIfNull(httpContext);

        ChatBotCorrelationContext context = httpContext.ResolveCorrelationContext();
        Activity.Current?.SetTag("hexalith.correlation_id", context.CorrelationId);
        if (context.TaskId is not null)
        {
            Activity.Current?.SetTag("hexalith.task_id", context.TaskId);
        }

        httpContext.Response.OnStarting(
            static state =>
            {
                HttpContext current = (HttpContext)state;
                ChatBotCorrelationContext resolved = current.GetCorrelationContext();
                current.Response.Headers["X-Correlation-Id"] = resolved.CorrelationId;
                if (resolved.TaskId is not null)
                {
                    current.Response.Headers["X-Hexalith-Task-Id"] = resolved.TaskId;
                }

                return Task.CompletedTask;
            },
            httpContext);

        // Bind the scope to a live view of the resolved correlation context rather than a snapshot of the
        // initially generated value. On the missing/invalid-header command path the effective correlationId is
        // only finalized later (commandId-derived fallback in the command endpoint); reading the context lazily
        // keeps every log line consistent with the response body, the X-Correlation-Id header, and the audit
        // envelope. Only parsed ULID metadata ever reaches the scope, so the metadata-only invariant holds.
        using IDisposable? scope = logger.BeginScope(new CorrelationLogScope(httpContext));

        await next(httpContext).ConfigureAwait(false);
    }

    private sealed class CorrelationLogScope(HttpContext httpContext) : IReadOnlyList<KeyValuePair<string, object>>
    {
        public int Count => 2;

        public KeyValuePair<string, object> this[int index] => index switch
        {
            0 => new("correlationId", httpContext.GetCorrelationContext().CorrelationId),
            1 => new("taskId", httpContext.GetCorrelationContext().TaskId ?? string.Empty),
            _ => throw new ArgumentOutOfRangeException(nameof(index)),
        };

        public IEnumerator<KeyValuePair<string, object>> GetEnumerator()
        {
            yield return this[0];
            yield return this[1];
        }

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        public override string ToString()
        {
            ChatBotCorrelationContext context = httpContext.GetCorrelationContext();
            return context.TaskId is null
                ? $"correlationId:{context.CorrelationId}"
                : $"correlationId:{context.CorrelationId} taskId:{context.TaskId}";
        }
    }
}
