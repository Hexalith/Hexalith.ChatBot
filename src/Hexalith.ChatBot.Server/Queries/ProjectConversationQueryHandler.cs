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

internal sealed class ProjectConversationQueryHandler(
    IProjectConversationProjectionStore projectionStore,
    IProjectAiContextPackageAssembler aiContextPackageAssembler,
    IQueryCursorCodec cursorCodec)
    : ChatBotReadQueryHandler<ProjectConversationQuery>
{
    public override string QueryType => ChatBotReadQueryTypes.ProjectConversation;

    protected override async Task<QueryResult> ExecuteAsync(QueryEnvelope query, ProjectConversationQuery request, CancellationToken cancellationToken)
    {
        if (!AuditMetadata.IsSafeStableIdentifier(request.ProjectId) || !request.ProjectReadAuthorized)
        {
            return QueryResult.Failure(ChatBotAuthorizationReasonCodes.SafeNotFound);
        }

        string scope = CursorScope(query.TenantId, request.ProjectId);
        if (!cursorCodec.TryDecode(request.Cursor, QueryType, scope, out string? cursorPositionText, out _))
        {
            return QueryResult.Failure(ChatBotAuthorizationReasonCodes.SafeNotFound);
        }

        if (!ProjectConversationCursorPosition.TryParse(cursorPositionText, out ProjectConversationCursorPosition? cursorPosition))
        {
            return QueryResult.Failure(ChatBotAuthorizationReasonCodes.SafeNotFound);
        }

        ProjectConversationPage page = await projectionStore
            .ReadPageAsync(query.TenantId, request.ProjectId, cursorPosition, request.PageSize, cancellationToken)
            .ConfigureAwait(false);
        IReadOnlyList<ProjectConversationItemView> aiContextPackageItems = await projectionStore
            .ReadAiContextPackageItemsAsync(query.TenantId, request.ProjectId, cancellationToken)
            .ConfigureAwait(false);
        ProjectAiContextPackage aiContextPackage = await aiContextPackageAssembler
            .AssembleAsync(
                new ProjectAiContextPackageAssemblyRequest(query.TenantId, request.ProjectId, aiContextPackageItems, query.CorrelationId),
                cancellationToken)
            .ConfigureAwait(false);

        if (page.Items.Count == 0 && !request.HasProjectScopeClaims)
        {
            return QueryResult.Failure(ChatBotAuthorizationReasonCodes.SafeNotFound);
        }

        string? nextCursor = ChatBotReadQueryResultMapper.EncodeNextCursor(
            page,
            position => cursorCodec.Encode(QueryType, scope, position.ToProtectedPosition()));
        ProjectConversationResponse response = ChatBotReadQueryResultMapper.BuildProjectConversationResponse(
            request.ProjectId,
            query.TenantId,
            page,
            nextCursor,
            query.CorrelationId,
            aiContextPackage);
        return Payload(response, "chatbot.project-conversation-response.v1");
    }

    private static string CursorScope(string tenantId, string projectId)
        => QueryCursorScope.Create()
            .Add("tenant", tenantId)
            .Add("project", projectId)
            .Add("query", ChatBotReadQueryTypes.ProjectConversation)
            .Build();
}
