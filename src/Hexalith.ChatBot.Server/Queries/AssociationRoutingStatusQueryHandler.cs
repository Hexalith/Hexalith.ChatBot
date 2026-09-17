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

internal sealed class AssociationRoutingStatusQueryHandler(IAssociationProjectionStore projectionStore)
    : ChatBotReadQueryHandler<AssociationRoutingStatusQuery>
{
    public override string QueryType => ChatBotReadQueryTypes.AssociationRoutingStatus;

    protected override async Task<QueryResult> ExecuteAsync(QueryEnvelope query, AssociationRoutingStatusQuery request, CancellationToken cancellationToken)
    {
        if (!AssociationWorkflowId.TryParse(request.AssociationId, out AssociationWorkflowId parsedAssociationId))
        {
            return QueryResult.Failure(ChatBotAuthorizationReasonCodes.SafeNotFound);
        }

        AssociationCandidateView? view = await projectionStore
            .GetAsync(query.TenantId, parsedAssociationId.Value, cancellationToken)
            .ConfigureAwait(false);

        return view is null
            ? QueryResult.Failure(ChatBotAuthorizationReasonCodes.SafeNotFound)
            : Payload(ChatBotReadQueryResultMapper.BuildAssociationRoutingStatus(view, query.CorrelationId), "chatbot.association-routing-status.v1");
    }
}
