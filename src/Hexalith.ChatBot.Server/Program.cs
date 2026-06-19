using System.Globalization;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text.Json;

using Hexalith.ChatBot.Client;
using Hexalith.ChatBot.Contracts.Commands;
using Hexalith.ChatBot.Contracts.Enums;
using Hexalith.ChatBot.Contracts.Identities;
using Hexalith.ChatBot.Contracts.Messages;
using Hexalith.ChatBot.Contracts.Queries;
using Hexalith.ChatBot.Server.Audit;
using Hexalith.ChatBot.Server.Authentication;
using Hexalith.ChatBot.Server.Gateway;
using Hexalith.ChatBot.Server.Gateway.Correlation;
using Hexalith.ChatBot.Server.Gateway.Stages;
using Hexalith.ChatBot.Server.Gateway.Status;
using Hexalith.ChatBot.Server.Governance.AiMediation;
using Hexalith.ChatBot.Server.Lifecycle.Attachments;
using Hexalith.ChatBot.Server.Lifecycle.Workflows;
using Hexalith.ChatBot.Server.Operations;
using Hexalith.ChatBot.Server.Operations.PeriodicEnforcement;
using Hexalith.ChatBot.Server.Projections;
using Hexalith.ChatBot.Server.Queries;
using Hexalith.ChatBot.ServiceDefaults;
using Hexalith.EventStore.Client.Registration;
using Hexalith.EventStore.Contracts.Projections;
using Hexalith.EventStore.Contracts.Queries;
using Hexalith.EventStore.DomainService;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

// AddEventStoreDomainService also wires shared Hexalith service defaults and owns the cross-signal OTLP exporter.
_ = builder.AddServiceDefaults(useOtlpExporterWhenConfigured: false);
_ = builder.Services.AddChatBotCommandGateway();
_ = builder.AddEventStoreDomainService(typeof(GovernedOperationAggregate).Assembly);
_ = builder.AddEventStoreDomainTelemetry("chatbot");
_ = builder.Services
    .AddHealthChecks()
    .AddEventStoreDomainStateStoreHealthCheck(
        "chatbot",
        stateStoreName: ChatBotReadModelStoreNames.StateStoreName,
        tags: ["ready", "chatbot"]);
_ = builder.Services.AddDataProtection();
_ = builder.Services.AddEventStoreQueryCursorCodec("Hexalith.ChatBot.QueryCursor.v1");
_ = builder.Services.Configure<PeriodicEnforcementOptions>(builder.Configuration.GetSection("ChatBot:PeriodicEnforcement"));

// JWT bearer auth is wired only when the topology supplies an Authority/SigningKey (the live Aspire Keycloak
// realm). The in-process WebApplicationFactory tests inject a test principal directly and configure neither, so
// no authentication middleware is added there and the injected principal is preserved.
bool jwtAuthentication = ChatBotJwtAuthentication.IsConfigured(builder.Configuration);
_ = builder.Services.AddChatBotJwtAuthentication(builder.Configuration);

if (string.Equals(builder.Configuration["ChatBot:UseDaprWorkflowRuntime"], "true", StringComparison.OrdinalIgnoreCase))
{
    _ = builder.Services.AddChatBotCorrectionPropagationWorkflow();
}

if (string.Equals(builder.Configuration["ChatBot:UsePeriodicEnforcementRuntime"], "true", StringComparison.OrdinalIgnoreCase))
{
    _ = builder.Services.Configure<PeriodicEnforcementOptions>(
        options => options.UsePeriodicEnforcementRuntime = true);
    _ = builder.Services.AddChatBotPeriodicEnforcementHostedService();
}

// Gate the durable DAPR-backed read-model store on a sidecar being present: the live topology sets
// ChatBot:UseDaprStateStores=true so the projection lands in chatbot-statestore; in-process tests keep the
// in-memory default (no sidecar).
if (string.Equals(builder.Configuration["ChatBot:UseDaprStateStores"], "true", StringComparison.OrdinalIgnoreCase))
{
    _ = builder.Services.AddChatBotDaprStateStores();
}

WebApplication app = builder.Build();

if (jwtAuthentication)
{
    _ = app.UseAuthentication();
}

_ = app.UseChatBotCorrelation();

// DAPR pub/sub delivery: UseCloudEvents unwraps the CloudEvent so the projection subscriber binds the
// EventStore-stamped envelope as the request body; MapSubscribeHandler exposes the declarative subscription
// registry. Harmless for the in-process tests (a plain application/json POST passes through unchanged).
_ = app.UseCloudEvents();
_ = app.MapSubscribeHandler();
_ = app.MapDefaultEndpoints();
_ = app.MapGet("/health/chatbot", () => Results.Ok(new ChatBotHealth(
    ChatBotClientDescriptor.Default.ModuleName,
    ChatBotClientDescriptor.Default.DaprAppId,
    ChatBotHealthStatuses.ToWireValue(ChatBotHealthStatus.Healthy))));
_ = app.MapGet(
    "/health/chatbot/workflows",
    async (ICorrectionPropagationWorkflowRuntime runtime, CancellationToken cancellationToken) =>
    {
        CorrectionPropagationWorkflowRuntimeStatus status = await runtime
            .CheckAsync(cancellationToken)
            .ConfigureAwait(false);
        return status.IsAvailable
            ? Results.Ok(status)
            : Results.Json(status, statusCode: StatusCodes.Status503ServiceUnavailable);
    });
_ = app.MapGet(
    "/health/chatbot/periodic-enforcement",
    (PeriodicEnforcementCoordinator coordinator) => Results.Ok(coordinator.Status));
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
        ChatBotSurfaceOrigin origin = ResolveSurfaceOrigin(wireRequest, httpContext);
        string? replayRunId = ResolveReplayRunId(httpContext);
        ChatBotGatewayResult result = await gateway
            .SubmitAsync(
                new ChatBotCommandSubmission(
                    httpContext.User,
                    request,
                    correlationContext.CorrelationId,
                    correlationContext.TaskId,
                    origin,
                    replayRunId),
                cancellationToken)
            .ConfigureAwait(false);

        return CommandGatewayHttpResults.ToHttpResult(result);
    });
_ = app.MapChatBotDomainServiceEndpoints();
_ = app.MapPost(
    "/query",
    async (QueryEnvelope query, IServiceProvider serviceProvider, CancellationToken cancellationToken) =>
        Results.Ok(await DomainQueryDispatcher.ExecuteAsync(serviceProvider, query, cancellationToken).ConfigureAwait(false)));
_ = app.MapPost(
    "/project",
    (ProjectionRequest request, IServiceProvider serviceProvider) =>
    {
        ProjectionResponse? response = DomainProjectionDispatcher.Project(serviceProvider, request);
        return response is null ? Results.NotFound() : Results.Ok(response);
    });

// The EventStore publishes chatbot events to "{tenantId}.chatbot.events" on the chatbot-pubsub component; the
// subscription topic is configurable so the M0 single-tenant topic is set by the topology without baking a
// tenant into code. Defaults keep the in-process tests independent of any sidecar.
_ = app.MapGovernedOperationProjectionEndpoints(
    app.Configuration["ChatBot:Projection:PubSubName"] ?? "chatbot-pubsub",
    app.Configuration["ChatBot:Projection:Topic"] ?? "chatbot.events");
_ = app.MapMailboxIntakeProjectionEndpoints(
    app.Configuration["ChatBot:Projection:PubSubName"] ?? "chatbot-pubsub",
    app.Configuration["ChatBot:Projection:Topic"] ?? "chatbot.events");
_ = app.MapAssociationProjectionEndpoints(
    app.Configuration["ChatBot:Projection:PubSubName"] ?? "chatbot-pubsub",
    app.Configuration["ChatBot:Projection:Topic"] ?? "chatbot.events");
_ = app.MapParticipantResolutionProjectionEndpoints(
    app.Configuration["ChatBot:Projection:PubSubName"] ?? "chatbot-pubsub",
    app.Configuration["ChatBot:Projection:Topic"] ?? "chatbot.events");
_ = app.MapAiOutcomeProjectionEndpoints(
    app.Configuration["ChatBot:Projection:PubSubName"] ?? "chatbot-pubsub",
    app.Configuration["ChatBot:Projection:Topic"] ?? "chatbot.events");
_ = app.MapTaskIntentProjectionEndpoints(
    app.Configuration["ChatBot:Projection:PubSubName"] ?? "chatbot-pubsub",
    app.Configuration["ChatBot:Projection:Topic"] ?? "chatbot.events");
_ = app.MapApprovalProjectionEndpoints(
    app.Configuration["ChatBot:Projection:PubSubName"] ?? "chatbot-pubsub",
    app.Configuration["ChatBot:Projection:Topic"] ?? "chatbot.events");
_ = app.MapGet(
    "/api/v1/associations/{associationId}/routing-status",
    async (
        string associationId,
        HttpContext httpContext,
        IServiceProvider serviceProvider,
        IChatBotProblemDetailsFactory problemDetailsFactory,
        CancellationToken cancellationToken) =>
    {
        ChatBotCorrelationContext correlationContext = httpContext.GetCorrelationContext();
        if (!AssociationWorkflowId.TryParse(associationId, out AssociationWorkflowId parsedAssociationId))
        {
            return CommandGatewayHttpResults.ToHttpResult(ChatBotGatewayResult.Denied(
                problemDetailsFactory.CreateAuthorizationProblem(
                    ChatBotAuthorizationReasonCodes.SafeNotFound,
                    correlationContext.CorrelationId,
                    correlationContext.TaskId)));
        }

        return await ExecuteReadQueryAsync(
            parsedAssociationId.Value,
            ChatBotReadQueryTypes.AssociationRoutingStatus,
            new AssociationRoutingStatusQuery(associationId, correlationContext.TaskId),
            httpContext,
            serviceProvider,
            problemDetailsFactory,
            cancellationToken).ConfigureAwait(false);
    });
_ = app.MapGet(
    "/api/v1/projects/{projectId}/conversation",
    async (
        string projectId,
        string? cursor,
        int? pageSize,
        HttpContext httpContext,
        IServiceProvider serviceProvider,
        IChatBotProblemDetailsFactory problemDetailsFactory,
        CancellationToken cancellationToken) =>
    {
        ChatBotCorrelationContext correlationContext = httpContext.GetCorrelationContext();
        if (!AuditMetadata.IsSafeStableIdentifier(projectId))
        {
            return CommandGatewayHttpResults.ToHttpResult(ChatBotGatewayResult.Denied(
                problemDetailsFactory.CreateAuthorizationProblem(
                    ChatBotAuthorizationReasonCodes.SafeNotFound,
                    correlationContext.CorrelationId,
                    correlationContext.TaskId)));
        }

        // Resolve tenant BEFORE project authorization so an unauthenticated read still collapses to the
        // AuthenticationDenied (401) signal the caller needs, exactly like the pre-migration host. Running the
        // project-scope check first would mask the unauthenticated case as a SafeNotFound (403) denial.
        if (!ChatBotReadAuthorization.TryResolveTenant(httpContext.User, out _, out _, out string tenantReasonCode))
        {
            return CommandGatewayHttpResults.ToHttpResult(ChatBotGatewayResult.Denied(
                problemDetailsFactory.CreateAuthorizationProblem(
                    ChatBotReadAuthorization.ReadDenialReason(tenantReasonCode),
                    correlationContext.CorrelationId,
                    correlationContext.TaskId)));
        }

        bool projectReadAuthorized = ChatBotReadAuthorization.TryAuthorizeProjectRead(httpContext.User, projectId, out bool hasProjectScopeClaims);
        if (!projectReadAuthorized)
        {
            return CommandGatewayHttpResults.ToHttpResult(ChatBotGatewayResult.Denied(
                problemDetailsFactory.CreateAuthorizationProblem(
                    ChatBotAuthorizationReasonCodes.SafeNotFound,
                    correlationContext.CorrelationId,
                    correlationContext.TaskId)));
        }

        return await ExecuteProjectConversationQueryAsync(
            projectId,
            new ProjectConversationQuery(projectId, cursor, Math.Clamp(pageSize ?? 25, 1, 100), projectReadAuthorized, hasProjectScopeClaims, correlationContext.TaskId),
            httpContext,
            serviceProvider,
            problemDetailsFactory,
            cancellationToken).ConfigureAwait(false);
    });
_ = app.MapGet(
    "/api/v1/projects/{projectId}/task-intents/{taskIntentId}",
    async (
        string projectId,
        string taskIntentId,
        HttpContext httpContext,
        IServiceProvider serviceProvider,
        IChatBotProblemDetailsFactory problemDetailsFactory,
        CancellationToken cancellationToken) =>
    {
        ChatBotCorrelationContext correlationContext = httpContext.GetCorrelationContext();
        if (!AuditMetadata.IsSafeStableIdentifier(projectId) ||
            !AuditMetadata.IsSafeStableIdentifier(taskIntentId))
        {
            return CommandGatewayHttpResults.ToHttpResult(ChatBotGatewayResult.Denied(
                problemDetailsFactory.CreateAuthorizationProblem(
                    ChatBotAuthorizationReasonCodes.SafeNotFound,
                    correlationContext.CorrelationId,
                    correlationContext.TaskId)));
        }

        // Resolve tenant BEFORE project authorization so an unauthenticated read still collapses to the
        // AuthenticationDenied (401) signal the caller needs, exactly like the pre-migration host. Running the
        // project-scope check first would mask the unauthenticated case as a SafeNotFound (403) denial.
        if (!ChatBotReadAuthorization.TryResolveTenant(httpContext.User, out _, out _, out string tenantReasonCode))
        {
            return CommandGatewayHttpResults.ToHttpResult(ChatBotGatewayResult.Denied(
                problemDetailsFactory.CreateAuthorizationProblem(
                    ChatBotReadAuthorization.ReadDenialReason(tenantReasonCode),
                    correlationContext.CorrelationId,
                    correlationContext.TaskId)));
        }

        bool projectReadAuthorized = ChatBotReadAuthorization.TryAuthorizeProjectRead(httpContext.User, projectId, out _);
        if (!projectReadAuthorized)
        {
            return CommandGatewayHttpResults.ToHttpResult(ChatBotGatewayResult.Denied(
                problemDetailsFactory.CreateAuthorizationProblem(
                    ChatBotAuthorizationReasonCodes.SafeNotFound,
                    correlationContext.CorrelationId,
                    correlationContext.TaskId)));
        }

        return await ExecuteReadQueryAsync(
            projectId,
            ChatBotReadQueryTypes.TaskIntentReview,
            new TaskIntentReviewQuery(projectId, taskIntentId, projectReadAuthorized, correlationContext.TaskId),
            httpContext,
            serviceProvider,
            problemDetailsFactory,
            cancellationToken).ConfigureAwait(false);
    });
_ = app.MapGet(
    "/api/v1/operations/{operationId}",
    async (
        string operationId,
        HttpContext httpContext,
        IServiceProvider serviceProvider,
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

        return await ExecuteReadQueryAsync(
            operationId,
            ChatBotReadQueryTypes.OperationStatus,
            new OperationStatusQuery(operationId, correlationContext.TaskId),
            httpContext,
            serviceProvider,
            problemDetailsFactory,
            cancellationToken).ConfigureAwait(false);
    });
_ = app.MapGet(
    "/api/v1/operations/{operationId}/audit-history",
    async (
        string operationId,
        HttpContext httpContext,
        IServiceProvider serviceProvider,
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

        return await ExecuteReadQueryAsync(
            operationId,
            ChatBotReadQueryTypes.OperationAuditHistory,
            new OperationAuditHistoryQuery(operationId, correlationContext.TaskId),
            httpContext,
            serviceProvider,
            problemDetailsFactory,
            cancellationToken).ConfigureAwait(false);
    });
_ = app.MapGet(
    "/api/v1/governed-operations/{noteId}",
    async (
        string noteId,
        HttpContext httpContext,
        IServiceProvider serviceProvider,
        IChatBotProblemDetailsFactory problemDetailsFactory,
        CancellationToken cancellationToken) =>
    {
        ChatBotCorrelationContext correlationContext = httpContext.GetCorrelationContext();
        if (!ChatBotIdentity.IsValidUlid(noteId))
        {
            return CommandGatewayHttpResults.ToHttpResult(ChatBotGatewayResult.Denied(
                problemDetailsFactory.CreateAuthorizationProblem(
                    ChatBotAuthorizationReasonCodes.SafeNotFound,
                    correlationContext.CorrelationId,
                    correlationContext.TaskId)));
        }

        return await ExecuteReadQueryAsync(
            noteId,
            ChatBotReadQueryTypes.GovernedOperation,
            new GovernedOperationQuery(noteId, correlationContext.TaskId),
            httpContext,
            serviceProvider,
            problemDetailsFactory,
            cancellationToken).ConfigureAwait(false);
    });
_ = app.MapPost(
    "/api/v1/compliance/audit/search",
    async (
        ComplianceAuditQueryFilters? query,
        HttpContext httpContext,
        IServiceProvider serviceProvider,
        IChatBotProblemDetailsFactory problemDetailsFactory,
        CancellationToken cancellationToken) =>
    {
        ChatBotCorrelationContext correlationContext = httpContext.GetCorrelationContext();
        return await ExecuteReadQueryAsync(
            query?.QueryRef ?? ChatBotReadQueryTypes.ComplianceAuditSearch,
            ChatBotReadQueryTypes.ComplianceAuditSearch,
            new ComplianceAuditSearchQuery(query, ChatBotReadAuthorization.CanSearchTenantAudit(httpContext.User), correlationContext.TaskId),
            httpContext,
            serviceProvider,
            problemDetailsFactory,
            cancellationToken).ConfigureAwait(false);
    });
_ = app.MapGet(
    "/api/v1/compliance/audit/{auditRecordRef}",
    async (
        string auditRecordRef,
        HttpContext httpContext,
        IServiceProvider serviceProvider,
        IChatBotProblemDetailsFactory problemDetailsFactory,
        CancellationToken cancellationToken) =>
    {
        ChatBotCorrelationContext correlationContext = httpContext.GetCorrelationContext();
        if (!ComplianceAdministrationSchema.IsSafeComplianceToken(auditRecordRef))
        {
            return CommandGatewayHttpResults.ToHttpResult(ChatBotGatewayResult.Denied(
                problemDetailsFactory.CreateAuthorizationProblem(
                    ChatBotAuthorizationReasonCodes.SafeNotFound,
                    correlationContext.CorrelationId,
                    correlationContext.TaskId)));
        }

        return await ExecuteReadQueryAsync(
            auditRecordRef,
            ChatBotReadQueryTypes.ComplianceAuditDetail,
            new ComplianceAuditDetailQuery(
                auditRecordRef,
                ChatBotReadAuthorization.CanSearchTenantAudit(httpContext.User),
                ChatBotReadAuthorization.ExplicitProjectGrants(httpContext.User),
                correlationContext.TaskId),
            httpContext,
            serviceProvider,
            problemDetailsFactory,
            cancellationToken).ConfigureAwait(false);
    });

app.Run();

static string NormalizeCommandId(string? value)
    => ChatBotCommandId.TryParse(value, out ChatBotCommandId commandId)
        ? commandId.Value
        : ChatBotCommandId.New().Value;

static async Task<IResult> ExecuteReadQueryAsync(
    string aggregateId,
    string queryType,
    object payload,
    HttpContext httpContext,
    IServiceProvider serviceProvider,
    IChatBotProblemDetailsFactory problemDetailsFactory,
    CancellationToken cancellationToken)
{
    QueryResult result = await DispatchReadQueryAsync(
        aggregateId,
        queryType,
        payload,
        httpContext,
        serviceProvider,
        problemDetailsFactory,
        cancellationToken)
        .ConfigureAwait(false);
    return result.Success
        ? Results.Bytes(result.PayloadBytes ?? [], contentType: "application/json")
        : Denied(httpContext.GetCorrelationContext(), problemDetailsFactory, result.ErrorMessage ?? ChatBotAuthorizationReasonCodes.SafeNotFound);
}

static async Task<IResult> ExecuteProjectConversationQueryAsync(
    string projectId,
    ProjectConversationQuery payload,
    HttpContext httpContext,
    IServiceProvider serviceProvider,
    IChatBotProblemDetailsFactory problemDetailsFactory,
    CancellationToken cancellationToken)
{
    QueryResult result = await DispatchReadQueryAsync(
        projectId,
        ChatBotReadQueryTypes.ProjectConversation,
        payload,
        httpContext,
        serviceProvider,
        problemDetailsFactory,
        cancellationToken)
        .ConfigureAwait(false);
    if (!result.Success)
    {
        return Denied(httpContext.GetCorrelationContext(), problemDetailsFactory, result.ErrorMessage ?? ChatBotAuthorizationReasonCodes.SafeNotFound);
    }

    ProjectConversationResponse? response = JsonSerializer.Deserialize<ProjectConversationResponse>(result.PayloadBytes ?? [], Program.QueryJsonOptions);
    return response is null
        ? Denied(httpContext.GetCorrelationContext(), problemDetailsFactory, ChatBotAuthorizationReasonCodes.SafeNotFound)
        : ChatBotReadQueryResultMapper.ProjectConversationHttpResult(httpContext, response);
}

static async Task<QueryResult> DispatchReadQueryAsync(
    string aggregateId,
    string queryType,
    object payload,
    HttpContext httpContext,
    IServiceProvider serviceProvider,
    IChatBotProblemDetailsFactory problemDetailsFactory,
    CancellationToken cancellationToken)
{
    ChatBotCorrelationContext correlationContext = httpContext.GetCorrelationContext();
    if (!ChatBotReadAuthorization.TryResolveTenant(httpContext.User, out string? tenantId, out string? userId, out string reasonCode))
    {
        return QueryResult.Failure(ChatBotReadAuthorization.ReadDenialReason(reasonCode));
    }

    QueryEnvelope envelope = new(
        tenantId!,
        ChatBotReadQueryTypes.Domain,
        aggregateId,
        queryType,
        JsonSerializer.SerializeToUtf8Bytes(payload, Program.QueryJsonOptions),
        correlationContext.CorrelationId,
        userId!);
    return await DomainQueryDispatcher.ExecuteAsync(serviceProvider, envelope, cancellationToken).ConfigureAwait(false);
}

static IResult Denied(
    ChatBotCorrelationContext correlationContext,
    IChatBotProblemDetailsFactory problemDetailsFactory,
    string reasonCode)
    => CommandGatewayHttpResults.ToHttpResult(ChatBotGatewayResult.Denied(
        problemDetailsFactory.CreateAuthorizationProblem(
            reasonCode,
            correlationContext.CorrelationId,
            correlationContext.TaskId)));

// Surface origin is captured once here at the adapter boundary (FR85 / S7): the request body field
// takes precedence, then the X-Hexalith-Surface-Origin header, and an absent/unknown declaration
// collapses to the safe default. From this point it is immutable on ChatBotCommandSubmission.
static ChatBotSurfaceOrigin ResolveSurfaceOrigin(CommandSubmissionWireRequest wireRequest, HttpContext httpContext)
{
    string? declared = wireRequest.Origin;
    if (string.IsNullOrWhiteSpace(declared)
        && httpContext.Request.Headers.TryGetValue("X-Hexalith-Surface-Origin", out Microsoft.Extensions.Primitives.StringValues header)
        && header.Count == 1)
    {
        declared = header[0];
    }

    return ChatBotSurfaceOrigins.FromWireValueOrDefault(declared);
}

// Story 9.4 (FR95a): the replay-run marker is captured once here at the adapter boundary — exactly like the surface
// origin — from the X-Hexalith-Replay-Run-Id header, sanitized to an AuditMetadata-safe bounded token. A production
// submission carries no such header, so the marker is null by omission; the replay run id is only ever set when an
// actual replay run supplies it. The replay-initiation surface (a QA/support UI or CLI) is deferred (inert-control
// floor) — this seam accepts and threads the value end-to-end so a replay run can be driven through the gateway today
// even without a dedicated initiator. The marker is non-binding for production tenants: isolation is enforced by the
// test-tenant adapter selection and the nightly probe, not by the presence/absence of this header.
static string? ResolveReplayRunId(HttpContext httpContext)
{
    if (httpContext.Request.Headers.TryGetValue("X-Hexalith-Replay-Run-Id", out Microsoft.Extensions.Primitives.StringValues header)
        && header.Count == 1)
    {
        return Hexalith.ChatBot.Server.Audit.AuditMetadata.SafeOptionalToken(header[0]);
    }

    return null;
}

public sealed record ChatBotHealth(string ModuleName, string DaprAppId, string Status);

public partial class Program
{
    internal static readonly JsonSerializerOptions QueryJsonOptions = new(JsonSerializerDefaults.Web);
}
