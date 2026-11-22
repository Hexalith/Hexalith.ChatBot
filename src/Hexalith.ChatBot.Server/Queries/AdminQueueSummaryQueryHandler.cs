using Hexalith.ChatBot.Contracts.Enums;
using Hexalith.ChatBot.Contracts.Identities;
using Hexalith.ChatBot.Contracts.Queries;
using Hexalith.ChatBot.Server.Audit;
using Hexalith.ChatBot.Server.Authentication;
using Hexalith.ChatBot.Server.Authorization;
using Hexalith.ChatBot.Server.Gateway;
using Hexalith.ChatBot.Server.Gateway.Stages;
using Hexalith.ChatBot.Server.Governance.Policy;
using Hexalith.ChatBot.Server.Projections;
using Hexalith.EventStore.Contracts.Queries;
using Hexalith.EventStore.Client.Streams;

namespace Hexalith.ChatBot.Server.Queries;

internal sealed class AdminQueueSummaryQueryHandler(ChatBotRequestContextResolver resolver, ChatBotRequestAuthorizer authorizer,
    ReviewQueueAggregateReader reader, IAuditWriter audit, ISystemClock clock)
    : ChatBotReadQueryHandler<GetAdminQueueSummary>(resolver, authorizer)
{
    public static Hexalith.EventStore.Contracts.Routing.RouteContractDeclaration CatalogDeclaration { get; } =
        Routing.ChatBotCatalogDeclarations.Query(ChatBotReadQueryTypes.AdminQueueSummary, typeof(AdminQueueSummaryQueryHandler));

    public override string QueryType => ChatBotReadQueryTypes.AdminQueueSummary;

    protected override async Task<QueryResult> ExecuteAsync(QueryEnvelope query, GetAdminQueueSummary request,
        ChatBotAuthorityPrincipal principal, CancellationToken cancellationToken)
    {
        if (request.ScopeUsed != AdminScope.SeeOnly || request.QueueRef is null || !request.QueueRef.StartsWith("queue:", StringComparison.Ordinal) ||
            !OperationalQueueFamilies.TryFromWireValue(request.QueueRef[6..], out var family) ||
            request.QueueRef != "queue:" + OperationalQueueFamilies.ToWireValue(family))
        { return QueryResult.Failure(ChatBotAuthorizationReasonCodes.SafeNotFound); }

        string scope = request.QueueRef;
        DateTimeOffset now = clock.UtcNow;
        var auditFact = new AuditEnvelope(query.TenantId, principal.Context.SubjectId, principal.Context.ActorClass,
            QueryType, scope, "allow", "queue_summary_read", query.CorrelationId, now, "queue-summary", [],
            null, "read", "metadata_only", "requested", AuditCommitPhase.PreCommit, "chatbot.audit-envelope.v1", null, "api");
        if (!(await audit.RecordPreCommitAsync(auditFact, cancellationToken).ConfigureAwait(false)).Succeeded)
        { return QueryResult.Failure("audit_unavailable"); }

        var observation = await reader.ReadAsync(query.TenantId, family, cancellationToken).ConfigureAwait(false);
        var auditRef = new AdminOperationReference(principal.Context.SubjectId, principal.Context.ActorClass, request.ScopeUsed,
            scope, [], 0, "queue_summary_read", observation.Policy?.Snapshot ?? "unavailable", now, 0, "metadata_only");
        var summary = AdminQueueSummaryProjector.Create(scope, observation.Items, auditRef,
            query.CorrelationId, observation.Policy?.Minimum, observation.Coverage);
        RequireCurrentAuthority(principal);
        return Payload(summary, "chatbot.admin-queue-summary.v1");
    }
}
