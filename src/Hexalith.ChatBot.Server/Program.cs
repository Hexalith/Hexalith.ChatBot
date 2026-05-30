using Hexalith.ChatBot.Client;
using Hexalith.ChatBot.Contracts.Identities;
using Hexalith.ChatBot.Server.Gateway;
using Hexalith.ChatBot.ServiceDefaults;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

_ = builder.AddServiceDefaults();
_ = builder.Services.AddChatBotCommandGateway();

WebApplication app = builder.Build();

_ = app.MapDefaultEndpoints();
_ = app.MapGet("/health/chatbot", () => Results.Ok(new ChatBotHealth(ChatBotClientDescriptor.Default.ModuleName, ChatBotClientDescriptor.Default.DaprAppId)));
_ = app.MapPost(
    "/api/v1/commands",
    async (
        CommandSubmissionWireRequest wireRequest,
        HttpContext httpContext,
        CommandGateway gateway,
        CancellationToken cancellationToken) =>
    {
        var request = wireRequest.ToGeneratedRequest();
        request.CommandId = NormalizeCommandId(request.CommandId);
        string correlationId = HeaderUlidOrFallback(httpContext, "X-Correlation-Id", request.CommandId);
        string? taskId = HeaderUlidOrNull(httpContext, "X-Hexalith-Task-Id");
        ChatBotGatewayResult result = await gateway
            .SubmitAsync(new ChatBotCommandSubmission(httpContext.User, request, correlationId, taskId), cancellationToken)
            .ConfigureAwait(false);

        return CommandGatewayHttpResults.ToHttpResult(result);
    });

app.Run();

static string NormalizeCommandId(string? value)
    => ChatBotCommandId.TryParse(value, out ChatBotCommandId commandId)
        ? commandId.Value
        : ChatBotCommandId.New().Value;

static string HeaderUlidOrFallback(HttpContext httpContext, string name, string fallback)
{
    string? value = HeaderValue(httpContext, name);
    return ChatBotCorrelationId.TryParse(value, out ChatBotCorrelationId correlationId)
        ? correlationId.Value
        : fallback;
}

static string? HeaderUlidOrNull(HttpContext httpContext, string name)
{
    string? value = HeaderValue(httpContext, name);
    return ChatBotTaskId.TryParse(value, out ChatBotTaskId taskId)
        ? taskId.Value
        : null;
}

static string? HeaderValue(HttpContext httpContext, string name)
    => httpContext.Request.Headers.TryGetValue(name, out Microsoft.Extensions.Primitives.StringValues values) &&
        values.Count == 1
            ? values[0]
            : null;

public sealed record ChatBotHealth(string ModuleName, string DaprAppId);

public partial class Program;
