using Hexalith.ChatBot.Contracts.Queries;
using Hexalith.ChatBot.Server.Audit;
using Hexalith.ChatBot.Server.Authentication;
using Hexalith.ChatBot.Server.Authorization;
using Hexalith.ChatBot.Server.Gateway;
using Hexalith.ChatBot.Server.Projections;
using Hexalith.EventStore.Contracts.Queries;

namespace Hexalith.ChatBot.Server.Queries;

internal sealed class ProjectReviewQueueQueryHandler(ChatBotRequestContextResolver resolver, ChatBotRequestAuthorizer authorizer,
    ISystemClock clock, [FromKeyedServices("review-queue-native")] Hexalith.EventStore.Client.Streams.IAuthoritativeEventStreamReader source, ReviewQueueStore? queues = null) : ChatBotReadQueryHandler<GetProjectReviewQueue>(resolver, authorizer)
{
    public static Hexalith.EventStore.Contracts.Routing.RouteContractDeclaration CatalogDeclaration { get; } =
        Routing.ChatBotCatalogDeclarations.Query(ChatBotReadQueryTypes.QueueItems, typeof(ProjectReviewQueueQueryHandler));

    public override string QueryType => ChatBotReadQueryTypes.QueueItems;

    protected override async Task<QueryResult> ExecuteAsync(QueryEnvelope query, GetProjectReviewQueue request,
        ChatBotAuthorityPrincipal principal, CancellationToken token)
    {
        if (!Enum.IsDefined(request.QueueFamily) || !principal.HasProject(request.ProjectId))
        { return QueryResult.Failure(ChatBotAuthorizationReasonCodes.SafeNotFound); }

        if (request.SourceStreamId is { } stream && !await RequestAuthorizer.HasQueueResourceAuthoritiesAsync(principal, [stream], QueryType, token).ConfigureAwait(false))
        { return QueryResult.Failure(ChatBotAuthorizationReasonCodes.SafeNotFound); }
        List<OperationalQueueRow> rows = [];
        var coverage = new ReviewQueueCoverage(request.SourceStreamId ?? request.ProjectId,
            queues is null ? "projection-unavailable" : "project-source-inventory-unverified", null, "operations-admin", "verify-queue-projection");
        if (queues is not null)
        {
            try
            {
                var subjects = await queues.ReadFamilyAsync(query.TenantId, request.QueueFamily, token).ConfigureAwait(false);
                foreach (var item in subjects)
                {
                    if (item.Current.Terminal || request.SourceStreamId is { } requested && !item.History.Any(f => f.StreamId == requested)) { continue; }
                    if (await queues.CanReadAsync(item, principal, RequestAuthorizer, QueryType, token, request.ProjectId).ConfigureAwait(false))
                    { rows.Add(AdminQueueSummaryProjector.ToOperationalRow(item.ToItem(clock.UtcNow))); }
                }
                if (request.SourceStreamId is { } sourceId)
                {
                    coverage = await queues.CheckSourceAsync(query.TenantId, sourceId, sourceId, source, token).ConfigureAwait(false);
                    if (coverage.Reason != "current") { rows.Clear(); }
                    var latest = await queues.ReadFamilyAsync(query.TenantId, request.QueueFamily, token).ConfigureAwait(false);
                    if (!ReviewQueueSubject.SameSnapshot(subjects, latest))
                    {
                        rows.Clear();
                        coverage = coverage with { Reason = "projection-changing", NextAction = "retry-read" };
                    }
                }
            }
            catch (Exception ex) when (ex is IOException or InvalidDataException or HttpRequestException or Dapr.DaprException or Grpc.Core.RpcException)
            {
                rows.Clear();
                coverage = coverage with { Reason = "projection-unavailable" };
            }
        }
        return Payload(new { Rows = rows, Coverage = coverage },
            "chatbot.project-review-queue.v1");
    }
}
