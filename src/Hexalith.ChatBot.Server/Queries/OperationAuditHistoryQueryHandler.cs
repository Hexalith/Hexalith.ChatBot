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

internal sealed class OperationAuditHistoryQueryHandler(
    IOperationStatusStore statusStore,
    IAuditHistoryReader auditHistoryReader)
    : ChatBotReadQueryHandler<OperationAuditHistoryQuery>
{
    public override string QueryType => ChatBotReadQueryTypes.OperationAuditHistory;

    protected override async Task<QueryResult> ExecuteAsync(QueryEnvelope query, OperationAuditHistoryQuery request, CancellationToken cancellationToken)
    {
        if (!ChatBotIdentity.IsValidUlid(request.OperationId))
        {
            return QueryResult.Failure(ChatBotAuthorizationReasonCodes.SafeNotFound);
        }

        OperationStatusRecord? record = await statusStore
            .TryGetAsync(query.TenantId, request.OperationId, cancellationToken)
            .ConfigureAwait(false);
        if (record is null)
        {
            return QueryResult.Failure(ChatBotAuthorizationReasonCodes.SafeNotFound);
        }

        IReadOnlyList<AuditEnvelope> postCommitEnvelopes = auditHistoryReader.GetPostCommitEnvelopes(query.TenantId, record.CommandId);
        return QueryResult.FromPayload(
            OperationAuditHistoryHttpResults.ToJsonElement(record.OperationId, record.AuditStatus, postCommitEnvelopes),
            "chatbot.operation-audit-history.v1");
    }
}
