using System.Text.Json;

using Dapr;

using Hexalith.ChatBot.Client;
using Hexalith.ChatBot.Contracts.Commands;
using Hexalith.ChatBot.Contracts.Enums;
using Hexalith.ChatBot.Contracts.Identities;
using Hexalith.ChatBot.Contracts.Messages;
using Hexalith.ChatBot.Contracts.Queries;
using Hexalith.ChatBot.Server.Audit;
using Hexalith.ChatBot.Server.Gateway.Correlation;
using Hexalith.ChatBot.Server.Gateway.Status;
using Hexalith.ChatBot.Server.Lifecycle.Attachments;
using Hexalith.ChatBot.Server.Lifecycle.Workflows;
using Hexalith.ChatBot.Server.Operations;
using Hexalith.ChatBot.Server.Operations.PeriodicEnforcement;
using Hexalith.ChatBot.Server.Projections;
using Hexalith.ChatBot.Server.Queries;
using Hexalith.EventStore.Contracts.Queries;
using Hexalith.EventStore.DomainService;

namespace Hexalith.ChatBot.Server.Gateway;

internal static class ChatBotCompatibilityEndpointExtensions
{
    public static WebApplication MapChatBotCompatibilityEndpoints(this WebApplication app)
    {
        ArgumentNullException.ThrowIfNull(app);

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

        MapReadCompatibilityEndpoints(app);
        return app;
    }

    public static WebApplication MapChatBotProjectionSubscriptionCompatibilityEndpoints(this WebApplication app)
    {
        ArgumentNullException.ThrowIfNull(app);

        string pubSubName = app.Configuration["ChatBot:Projection:PubSubName"] ?? "chatbot-pubsub";
        string topic = app.Configuration["ChatBot:Projection:Topic"] ?? "chatbot.events";
        _ = app.MapSubscribeHandler();
        _ = app.MapGovernedOperationProjectionEndpoints(pubSubName, topic);
        _ = app.MapMailboxIntakeProjectionEndpoints(pubSubName, topic);
        _ = app.MapAssociationProjectionEndpoints(pubSubName, topic);
        _ = app.MapParticipantResolutionProjectionEndpoints(pubSubName, topic);
        _ = app.MapAiOutcomeProjectionEndpoints(pubSubName, topic);
        _ = app.MapTaskIntentProjectionEndpoints(pubSubName, topic);
        _ = app.MapApprovalProjectionEndpoints(pubSubName, topic);

        return app;
    }

    private static void MapReadCompatibilityEndpoints(WebApplication app)
    {
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
    }

    private static string NormalizeCommandId(string? value)
        => ChatBotCommandId.TryParse(value, out ChatBotCommandId commandId)
            ? commandId.Value
            : ChatBotCommandId.New().Value;

    private static async Task<IResult> ExecuteReadQueryAsync(
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
            cancellationToken)
            .ConfigureAwait(false);
        return result.Success
            ? Results.Bytes(result.PayloadBytes ?? [], contentType: "application/json")
            : Denied(httpContext.GetCorrelationContext(), problemDetailsFactory, result.ErrorMessage ?? ChatBotAuthorizationReasonCodes.SafeNotFound);
    }

    private static async Task<IResult> ExecuteProjectConversationQueryAsync(
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

    private static async Task<QueryResult> DispatchReadQueryAsync(
        string aggregateId,
        string queryType,
        object payload,
        HttpContext httpContext,
        IServiceProvider serviceProvider,
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

    private static IResult Denied(
        ChatBotCorrelationContext correlationContext,
        IChatBotProblemDetailsFactory problemDetailsFactory,
        string reasonCode)
        => CommandGatewayHttpResults.ToHttpResult(ChatBotGatewayResult.Denied(
            problemDetailsFactory.CreateAuthorizationProblem(
                reasonCode,
                correlationContext.CorrelationId,
                correlationContext.TaskId)));

    private static ChatBotSurfaceOrigin ResolveSurfaceOrigin(CommandSubmissionWireRequest wireRequest, HttpContext httpContext)
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

    private static string? ResolveReplayRunId(HttpContext httpContext)
    {
        if (httpContext.Request.Headers.TryGetValue("X-Hexalith-Replay-Run-Id", out Microsoft.Extensions.Primitives.StringValues header)
            && header.Count == 1)
        {
            return AuditMetadata.SafeOptionalToken(header[0]);
        }

        return null;
    }
}

public sealed record ChatBotHealth(string ModuleName, string DaprAppId, string Status);
