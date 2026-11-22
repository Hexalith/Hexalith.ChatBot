using Hexalith.ChatBot.Contracts.Enums;
using Hexalith.ChatBot.Contracts.Queries;
using Hexalith.ChatBot.Server.Audit;
using Hexalith.ChatBot.Server.Authentication;
using Hexalith.ChatBot.Server.Authorization;
using Hexalith.ChatBot.Server.Gateway;
using Hexalith.ChatBot.Server.Projections;
using Hexalith.EventStore.Contracts.Queries;

namespace Hexalith.ChatBot.Server.Queries;

internal sealed class ReviewQueueDetailQueryHandler(ChatBotRequestContextResolver resolver, ChatBotRequestAuthorizer authorizer,
    ISystemClock clock, ReviewQueueStore? queues = null) : ChatBotReadQueryHandler<GetOperationalQueueItemDetail>(resolver, authorizer)
{
    public static Hexalith.EventStore.Contracts.Routing.RouteContractDeclaration CatalogDeclaration { get; } =
        Routing.ChatBotCatalogDeclarations.Query(ChatBotReadQueryTypes.QueueItemDetail, typeof(ReviewQueueDetailQueryHandler));

    public override string QueryType => ChatBotReadQueryTypes.QueueItemDetail;

    protected override async Task<QueryResult> ExecuteAsync(QueryEnvelope query, GetOperationalQueueItemDetail request,
        ChatBotAuthorityPrincipal principal, CancellationToken token)
    {
        if (queues is null || !Enum.IsDefined(request.QueueFamily) || request.QueueRef != "queue:" + OperationalQueueFamilies.ToWireValue(request.QueueFamily))
        { return QueryResult.Failure(ChatBotAuthorizationReasonCodes.SafeNotFound); }
        var item = await queues.ReadAsync(query.TenantId, request.ItemRef, token).ConfigureAwait(false);
        if (item is null || item.Current.TenantId != query.TenantId || item.Current.Family != request.QueueFamily ||
            item.Current.Version != request.SourceVersion ||
            (request.Revision is null ? item.History.Any(f => f.StreamId != item.Current.StreamId) : request.Revision != item.Revision) || item.Current.ItemRef != request.ItemRef ||
            !await queues.CanReadAsync(item, principal, RequestAuthorizer, QueryType, token).ConfigureAwait(false))
        { return QueryResult.Failure(ChatBotAuthorizationReasonCodes.SafeNotFound); }

        var detail = AdminQueueSummaryProjector.CreateSafeDetail(AdminQueueSummaryProjector.ToOperationalRow(item.ToItem(clock.UtcNow)), true);
        return Payload(detail with { History = ReviewQueueSubject.Chronological(item.History).Select(fact => new ReviewQueueHistoryEntry(fact.State, fact.At,
            fact.Terminal, fact.Reason, fact.OperationId, fact.CorrelationId, fact.Version, fact.RetryCount, fact.StreamId, fact.Actor)).ToArray() }, "chatbot.review-queue-detail.v1");
    }
}
