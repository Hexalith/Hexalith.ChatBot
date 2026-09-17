using System.Security.Claims;
using System.Text.Json;

using Hexalith.ChatBot.Contracts.Commands;
using Hexalith.ChatBot.Contracts.Identities;
using Hexalith.ChatBot.Contracts.Queries;
using Hexalith.ChatBot.Server.Audit;
using Hexalith.ChatBot.Server.Gateway;
using Hexalith.ChatBot.Server.Gateway.Status;
using Hexalith.ChatBot.Server.Gateway.Stages;
using Hexalith.ChatBot.Server.Governance.AiMediation;
using Hexalith.ChatBot.Server.Lifecycle.Attachments;
using Hexalith.ChatBot.Server.Projections;
using Hexalith.EventStore.Client.Queries;
using Hexalith.EventStore.Contracts.Queries;
using Hexalith.EventStore.DomainService;

namespace Hexalith.ChatBot.Server.Queries;

internal sealed class TaskIntentReviewQueryHandler(
    IProjectConversationProjectionStore projectionStore,
    IMailboxMessageContentSource messageContentSource)
    : ChatBotReadQueryHandler<TaskIntentReviewQuery>
{
    public override string QueryType => ChatBotReadQueryTypes.TaskIntentReview;

    protected override async Task<QueryResult> ExecuteAsync(QueryEnvelope query, TaskIntentReviewQuery request, CancellationToken cancellationToken)
    {
        if (!AuditMetadata.IsSafeStableIdentifier(request.ProjectId) ||
            !AuditMetadata.IsSafeStableIdentifier(request.TaskIntentId) ||
            !request.ProjectReadAuthorized)
        {
            return QueryResult.Failure(ChatBotAuthorizationReasonCodes.SafeNotFound);
        }

        TaskIntentRecord? record = await projectionStore
            .GetTaskIntentAsync(query.TenantId, request.ProjectId, request.TaskIntentId, cancellationToken)
            .ConfigureAwait(false);
        if (record is null)
        {
            return Payload(
                ChatBotReadQueryResultMapper.TaskIntentReviewUnavailable(request.ProjectId, request.TaskIntentId, TaskIntentReasonCodes.MissingCapturedIntent, query.CorrelationId),
                "chatbot.task-intent-review.v1");
        }

        if (record.ConversionReadinessBlocked)
        {
            return Payload(
                ChatBotReadQueryResultMapper.TaskIntentReviewUnavailable(request.ProjectId, request.TaskIntentId, TaskIntentReasonCodes.StaleCorrectedContext, query.CorrelationId),
                "chatbot.task-intent-review.v1");
        }

        MailboxMessageContentResult source = await messageContentSource
            .GetAsync(query.TenantId, request.ProjectId, record.SourceMessageId, cancellationToken)
            .ConfigureAwait(false);
        return !source.Available || string.IsNullOrWhiteSpace(source.Content)
            ? Payload(ChatBotReadQueryResultMapper.TaskIntentReviewUnavailable(request.ProjectId, request.TaskIntentId, source.ReasonCode, query.CorrelationId), "chatbot.task-intent-review.v1")
            : Payload(ChatBotReadQueryResultMapper.BuildTaskIntentReview(record, source, query.CorrelationId), "chatbot.task-intent-review.v1");
    }
}
