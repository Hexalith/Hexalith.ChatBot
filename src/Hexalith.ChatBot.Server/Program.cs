using System.Security.Claims;

using Hexalith.ChatBot.Client;
using Hexalith.ChatBot.Contracts.Identities;
using Hexalith.ChatBot.Server.Audit;
using Hexalith.ChatBot.Server.Gateway;
using Hexalith.ChatBot.Server.Gateway.Correlation;
using Hexalith.ChatBot.Server.Gateway.Status;
using Hexalith.ChatBot.ServiceDefaults;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

_ = builder.AddServiceDefaults();
_ = builder.Services.AddChatBotCommandGateway();

WebApplication app = builder.Build();

_ = app.UseChatBotCorrelation();
_ = app.MapDefaultEndpoints();
_ = app.MapGet("/health/chatbot", () => Results.Ok(new ChatBotHealth(
    ChatBotClientDescriptor.Default.ModuleName,
    ChatBotClientDescriptor.Default.DaprAppId,
    "healthy")));
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
        ChatBotCorrelationContext correlationContext = httpContext.ResolveCorrelationContext(request.CommandId);
        ChatBotGatewayResult result = await gateway
            .SubmitAsync(
                new ChatBotCommandSubmission(
                    httpContext.User,
                    request,
                    correlationContext.CorrelationId,
                    correlationContext.TaskId),
                cancellationToken)
            .ConfigureAwait(false);

        return CommandGatewayHttpResults.ToHttpResult(result);
    });
_ = app.MapGet(
    "/api/v1/operations/{operationId}",
    async (
        string operationId,
        HttpContext httpContext,
        IOperationStatusStore statusStore,
        IChatBotProblemDetailsFactory problemDetailsFactory,
        CancellationToken cancellationToken) =>
    {
        ChatBotCorrelationContext correlationContext = httpContext.GetCorrelationContext();
        if (!ChatBotIdentity.IsValidUlid(operationId))
        {
            return CommandGatewayHttpResults.ToHttpResult(ChatBotGatewayResult.Denied(
                problemDetailsFactory.CreateAuthorizationProblem(
                    ChatBotAuthorizationReasonCodes.SafeNotFound,
                    correlationContext.CorrelationId,
                    correlationContext.TaskId)));
        }

        if (!TryResolveTenant(httpContext.User, out string? tenantId, out string reasonCode))
        {
            return CommandGatewayHttpResults.ToHttpResult(ChatBotGatewayResult.Denied(
                problemDetailsFactory.CreateAuthorizationProblem(reasonCode, correlationContext.CorrelationId, correlationContext.TaskId)));
        }

        OperationStatusRecord? record = await statusStore
            .TryGetAsync(tenantId!, operationId, cancellationToken)
            .ConfigureAwait(false);

        if (record is null)
        {
            return CommandGatewayHttpResults.ToHttpResult(ChatBotGatewayResult.Denied(
                problemDetailsFactory.CreateAuthorizationProblem(
                    ChatBotAuthorizationReasonCodes.SafeNotFound,
                    correlationContext.CorrelationId,
                    correlationContext.TaskId)));
        }

        return OperationStatusHttpResults.Ok(record);
    });

app.Run();

static string NormalizeCommandId(string? value)
    => ChatBotCommandId.TryParse(value, out ChatBotCommandId commandId)
        ? commandId.Value
        : ChatBotCommandId.New().Value;

static bool TryResolveTenant(ClaimsPrincipal principal, out string? tenantId, out string reasonCode)
{
    tenantId = null;
    reasonCode = ChatBotAuthorizationReasonCodes.AuthenticationDenied;

    if (principal.Identity is not ClaimsIdentity identity || !identity.IsAuthenticated)
    {
        return false;
    }

    string? actorId = principal.FindFirstValue("sub");
    if (!AuditMetadata.IsSafeStableIdentifier(actorId))
    {
        return false;
    }

    string[] tenantClaims = ["eventstore:tenant", "tenant"];
    string[] tenants = tenantClaims
        .SelectMany(principal.FindAll)
        .Select(static claim => claim.Value)
        .Where(AuditMetadata.IsSafeStableIdentifier)
        .Distinct(StringComparer.Ordinal)
        .ToArray();

    if (tenants.Length != 1)
    {
        reasonCode = ChatBotAuthorizationReasonCodes.TenantMissing;
        return false;
    }

    tenantId = tenants[0];
    reasonCode = string.Empty;
    return true;
}

public sealed record ChatBotHealth(string ModuleName, string DaprAppId, string Status);

public partial class Program;
