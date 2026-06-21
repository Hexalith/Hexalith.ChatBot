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

internal abstract class ChatBotReadQueryHandler<TRequest> : IDomainQueryHandler
{
    protected static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public string Domain => ChatBotReadQueryTypes.Domain;

    public abstract string QueryType { get; }

    public async Task<QueryResult> ExecuteAsync(QueryEnvelope query, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);
        TRequest? request = JsonSerializer.Deserialize<TRequest>(query.Payload, JsonOptions);
        return request is null
            ? QueryResult.Failure(ChatBotAuthorizationReasonCodes.SafeNotFound)
            : await ExecuteAsync(query, request, cancellationToken).ConfigureAwait(false);
    }

    protected static QueryResult Payload<T>(T payload, string projectionType)
        => QueryResult.FromPayload(JsonSerializer.SerializeToElement(payload, JsonOptions), projectionType);

    protected abstract Task<QueryResult> ExecuteAsync(QueryEnvelope query, TRequest request, CancellationToken cancellationToken);
}

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

internal sealed class OperationStatusQueryHandler(IOperationStatusStore statusStore)
    : ChatBotReadQueryHandler<OperationStatusQuery>
{
    public override string QueryType => ChatBotReadQueryTypes.OperationStatus;

    protected override async Task<QueryResult> ExecuteAsync(QueryEnvelope query, OperationStatusQuery request, CancellationToken cancellationToken)
    {
        if (!ChatBotIdentity.IsValidUlid(request.OperationId))
        {
            return QueryResult.Failure(ChatBotAuthorizationReasonCodes.SafeNotFound);
        }

        OperationStatusRecord? record = await statusStore
            .TryGetAsync(query.TenantId, request.OperationId, cancellationToken)
            .ConfigureAwait(false);

        return record is null
            ? QueryResult.Failure(ChatBotAuthorizationReasonCodes.SafeNotFound)
            : QueryResult.FromPayload(OperationStatusHttpResults.ToJsonElement(record), "chatbot.operation-status.v1");
    }
}

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

internal sealed class GovernedOperationQueryHandler(IGovernedOperationProjectionStore projectionStore)
    : ChatBotReadQueryHandler<GovernedOperationQuery>
{
    public override string QueryType => ChatBotReadQueryTypes.GovernedOperation;

    protected override async Task<QueryResult> ExecuteAsync(QueryEnvelope query, GovernedOperationQuery request, CancellationToken cancellationToken)
    {
        if (!ChatBotIdentity.IsValidUlid(request.NoteId))
        {
            return QueryResult.Failure(ChatBotAuthorizationReasonCodes.SafeNotFound);
        }

        GovernedOperationView? view = await projectionStore
            .GetAsync(query.TenantId, request.NoteId, cancellationToken)
            .ConfigureAwait(false);

        return view is null
            ? QueryResult.Failure(ChatBotAuthorizationReasonCodes.SafeNotFound)
            : Payload(GovernedOperationViewResponse.From(view), "chatbot.governed-operation.v1");
    }
}

internal sealed class ComplianceAuditSearchQueryHandler(IWormAuditStore wormAuditStore)
    : ChatBotReadQueryHandler<ComplianceAuditSearchQuery>
{
    public override string QueryType => ChatBotReadQueryTypes.ComplianceAuditSearch;

    protected override Task<QueryResult> ExecuteAsync(QueryEnvelope query, ComplianceAuditSearchQuery request, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (!request.CanSearchTenantAudit ||
            !ComplianceAdministrationSchema.ValidateAuditQueryFilters(request.Filters).IsValid)
        {
            return Task.FromResult(QueryResult.Failure(ChatBotAuthorizationReasonCodes.SafeNotFound));
        }

        IReadOnlyList<AuditEnvelope> envelopes =
            [.. wormAuditStore.EnumerateChain(query.TenantId).Select(static record => record.Envelope)];
        ComplianceAuditSearchResult result = ComplianceAuditReadPolicy.Search(
            ComplianceSearchPrincipal(),
            request.Filters!,
            envelopes,
            DateTimeOffset.UtcNow,
            query.CorrelationId);
        return Task.FromResult(QueryResult.FromPayload(ComplianceAuditHttpResults.SearchJsonElement(result), "chatbot.compliance-audit-search.v1"));
    }

    private static ClaimsPrincipal ComplianceSearchPrincipal()
        => new(new ClaimsIdentity(
            [
                new Claim(ParticipantAuthorizationStage.ActorTypeClaim, ParticipantAuthorizationStage.HumanActorValue),
                new Claim(ParticipantAuthorizationStage.TenantRoleClaim, "compliance-admin"),
            ],
            authenticationType: "query-snapshot"));
}

internal sealed class ComplianceAuditDetailQueryHandler(IWormAuditStore wormAuditStore)
    : ChatBotReadQueryHandler<ComplianceAuditDetailQuery>
{
    public override string QueryType => ChatBotReadQueryTypes.ComplianceAuditDetail;

    protected override Task<QueryResult> ExecuteAsync(QueryEnvelope query, ComplianceAuditDetailQuery request, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (!request.CanSearchTenantAudit || !ComplianceAdministrationSchema.IsSafeComplianceToken(request.AuditRecordRef))
        {
            return Task.FromResult(QueryResult.Failure(ChatBotAuthorizationReasonCodes.SafeNotFound));
        }

        AuditEnvelope? envelope = wormAuditStore.EnumerateChain(query.TenantId)
            .Select(static record => record.Envelope)
            .Where(static candidate => !AuditReplayExclusion.IsReplayEnvelope(candidate))
            .FirstOrDefault(candidate =>
                AuditMetadata.IsSafeStableIdentifier(candidate.ResourceId) &&
                string.Equals(candidate.ResourceId, request.AuditRecordRef, StringComparison.Ordinal));

        if (envelope is null)
        {
            return Task.FromResult(QueryResult.Failure(ChatBotAuthorizationReasonCodes.SafeNotFound));
        }

        bool hasPerProjectAuthority = envelope.SourceEvidenceRefs
            .Where(static reference => reference.StartsWith("project:", StringComparison.Ordinal))
            .Select(static reference => reference["project:".Length..])
            .Where(AuditMetadata.IsSafeStableIdentifier)
            .Any(projectRef => request.ExplicitProjectGrants.Contains(projectRef, StringComparer.Ordinal));
        ComplianceAuditDetail detail = ComplianceAuditReadPolicy.Detail(envelope, hasPerProjectAuthority);
        return Task.FromResult(QueryResult.FromPayload(ComplianceAuditHttpResults.DetailJsonElement(detail), "chatbot.compliance-audit-detail.v1"));
    }
}
