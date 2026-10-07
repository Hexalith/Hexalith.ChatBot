using System.Text.Json;

using Hexalith.ChatBot.Contracts.Commands;
using Hexalith.ChatBot.Contracts.Identities;
using Hexalith.ChatBot.Contracts.Queries;
using Hexalith.ChatBot.Server.Audit;
using Hexalith.ChatBot.Server.Authentication;
using Hexalith.ChatBot.Server.Authorization;
using Hexalith.ChatBot.Server.Gateway.Stages;
using Hexalith.ChatBot.Server.Gateway.Status;
using Hexalith.ChatBot.Server.Gateway;
using Hexalith.ChatBot.Server.Governance.AiMediation;
using Hexalith.ChatBot.Server.Lifecycle.Attachments;
using Hexalith.ChatBot.Server.Lifecycle.Workflows;
using Hexalith.ChatBot.Server.Projections;
using Hexalith.EventStore.Client.Queries;
using Hexalith.EventStore.Contracts.Queries;
using Hexalith.EventStore.DomainService;

namespace Hexalith.ChatBot.Server.Queries;

internal sealed class OperationStatusQueryHandler(
    ChatBotRequestContextResolver requestContextResolver,
    ChatBotRequestAuthorizer requestAuthorizer,
    IOperationStatusStore statusStore,
    ISystemClock? clock = null,
    ICorrectionPropagationWorkflowRuntime? workflowRuntime = null)
    : ChatBotReadQueryHandler<OperationStatusQuery>(requestContextResolver, requestAuthorizer)
{
    public override string QueryType => ChatBotReadQueryTypes.OperationStatus;

    protected override async Task<QueryResult> ExecuteAsync(QueryEnvelope query, OperationStatusQuery request, ChatBotAuthorityPrincipal principal, CancellationToken cancellationToken)
    {
        if (!ChatBotIdentity.IsValidUlid(request.OperationId))
        {
            return QueryResult.Failure(ChatBotAuthorizationReasonCodes.SafeNotFound);
        }

        OperationStatusRecord? record = await statusStore
            .TryGetAsync(query.TenantId, request.OperationId, cancellationToken)
            .ConfigureAwait(false);

        if (record?.NextRetryAt is not null && record.WorkflowInstanceId is { } instanceId)
        {
            RequireCurrentAuthority(principal);
            CorrectionPropagationWorkflowProgress? progress = workflowRuntime is { IsAvailable: true, HasAuthoritativeProgress: true }
                ? await workflowRuntime.ReadProgressAsync(instanceId, cancellationToken).ConfigureAwait(false)
                : null;
            if (progress is null || progress.Status != CorrectionPropagationWorkflowStatuses.Retrying ||
                progress.WorkflowInstanceId != instanceId || progress.TenantId != query.TenantId ||
                progress.CorrelationId != record.CorrelationId || progress.RetryCount != record.WorkflowRetryCount ||
                progress.RetryDueAt != record.NextRetryAt)
            {
                record = record with { NextRetryAt = null };
            }
        }

        return record is null
            ? QueryResult.Failure(ChatBotAuthorizationReasonCodes.SafeNotFound)
            : QueryResult.FromPayload(OperationStatusHttpResults.ToJsonElement(record, clock?.UtcNow), "chatbot.operation-status.v1");
    }
}
