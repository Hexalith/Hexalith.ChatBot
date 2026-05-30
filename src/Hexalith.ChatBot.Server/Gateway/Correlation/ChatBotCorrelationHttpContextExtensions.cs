using Hexalith.ChatBot.Contracts.Identities;

namespace Hexalith.ChatBot.Server.Gateway.Correlation;

internal static class ChatBotCorrelationHttpContextExtensions
{
    private const string ContextItemKey = "Hexalith.ChatBot.CorrelationContext";

    public static ChatBotCorrelationContext ResolveCorrelationContext(this HttpContext httpContext, string? fallbackCorrelationId = null)
    {
        ArgumentNullException.ThrowIfNull(httpContext);

        if (httpContext.Items[ContextItemKey] is ChatBotCorrelationContext existing)
        {
            if (ShouldReplaceGeneratedFallback(httpContext, fallbackCorrelationId, out ChatBotCorrelationContext? replacement))
            {
                httpContext.Items[ContextItemKey] = replacement;
                return replacement!;
            }

            return existing;
        }

        ChatBotCorrelationContext context = Create(httpContext, fallbackCorrelationId);
        httpContext.Items[ContextItemKey] = context;
        return context;
    }

    public static ChatBotCorrelationContext GetCorrelationContext(this HttpContext httpContext)
        => httpContext.Items[ContextItemKey] is ChatBotCorrelationContext context
            ? context
            : httpContext.ResolveCorrelationContext();

    private static ChatBotCorrelationContext Create(HttpContext httpContext, string? fallbackCorrelationId)
    {
        string? correlationHeader = HeaderValue(httpContext, "X-Correlation-Id");
        string correlationId = ChatBotCorrelationId.TryParse(correlationHeader, out ChatBotCorrelationId parsedCorrelationId)
            ? parsedCorrelationId.Value
            : FallbackCorrelationId(fallbackCorrelationId);

        string? taskHeader = HeaderValue(httpContext, "X-Hexalith-Task-Id");
        string? taskId = ChatBotTaskId.TryParse(taskHeader, out ChatBotTaskId parsedTaskId)
            ? parsedTaskId.Value
            : null;

        return new ChatBotCorrelationContext(correlationId, taskId);
    }

    private static bool ShouldReplaceGeneratedFallback(
        HttpContext httpContext,
        string? fallbackCorrelationId,
        out ChatBotCorrelationContext? replacement)
    {
        replacement = null;
        string? correlationHeader = HeaderValue(httpContext, "X-Correlation-Id");
        if (ChatBotCorrelationId.TryParse(correlationHeader, out _) ||
            !ChatBotCorrelationId.TryParse(fallbackCorrelationId, out ChatBotCorrelationId parsedFallback))
        {
            return false;
        }

        string? taskHeader = HeaderValue(httpContext, "X-Hexalith-Task-Id");
        string? taskId = ChatBotTaskId.TryParse(taskHeader, out ChatBotTaskId parsedTaskId)
            ? parsedTaskId.Value
            : null;
        replacement = new ChatBotCorrelationContext(parsedFallback.Value, taskId);
        System.Diagnostics.Activity.Current?.SetTag("hexalith.correlation_id", parsedFallback.Value);
        return true;
    }

    private static string FallbackCorrelationId(string? fallbackCorrelationId)
        => ChatBotCorrelationId.TryParse(fallbackCorrelationId, out ChatBotCorrelationId parsedFallback)
            ? parsedFallback.Value
            : ChatBotCorrelationId.New().Value;

    private static string? HeaderValue(HttpContext httpContext, string name)
        => httpContext.Request.Headers.TryGetValue(name, out Microsoft.Extensions.Primitives.StringValues values) &&
            values.Count == 1
                ? values[0]
                : null;
}
