using Hexalith.ChatBot.Contracts.Enums;
using Hexalith.ChatBot.Contracts.Queries;
using Hexalith.ChatBot.Server.Audit;
using Hexalith.ChatBot.Server.Governance.Policy;
using Hexalith.EventStore.Client.Streams;

namespace Hexalith.ChatBot.Server.Projections;

/// <summary>Verifies policy and contributing streams consistently for aggregate reads and worker telemetry.</summary>
internal sealed class ReviewQueueAggregateReader(ReviewQueueDisclosurePolicy policy, [FromKeyedServices("review-queue-native")] IAuthoritativeEventStreamReader source,
    ISystemClock clock, ReviewQueueStore? queues = null)
{
    public async Task<(IReadOnlyList<AdminQueueSummaryProjectionItem> Items, (int Minimum, string Snapshot)? Policy, ReviewQueueCoverage Coverage)> ReadAsync(
        string tenant, OperationalQueueFamily family, CancellationToken cancellationToken)
    {
        string scope = "queue:" + OperationalQueueFamilies.ToWireValue(family);
        IReadOnlyList<ReviewQueueSubject> subjects = [];
        var coverage = new ReviewQueueCoverage(scope, queues is null ? "projection-unavailable" : "tenant-source-inventory-unverified",
            null, "operations-admin", "verify-queue-projection");
        var currentPolicy = await policy.ReadAsync(tenant, clock, cancellationToken).ConfigureAwait(false);
        if (queues is not null && currentPolicy is not null)
        {
            try
            {
                subjects = await queues.ReadFamilyAsync(tenant, family, cancellationToken).ConfigureAwait(false);
                foreach (string stream in subjects.SelectMany(item => item.History).Select(f => f.StreamId).Distinct(StringComparer.Ordinal))
                {
                    var check = await queues.CheckSourceAsync(tenant, stream, scope, source, cancellationToken).ConfigureAwait(false);
                    if (check.Reason != "current")
                    {
                        subjects = [];
                        coverage = coverage with { Reason = "coverage-unverified", NextAction = "verify-and-replay-sources" };
                        break;
                    }
                }
                var latest = await queues.ReadFamilyAsync(tenant, family, cancellationToken).ConfigureAwait(false);
                if (!ReviewQueueSubject.SameSnapshot(subjects, latest)) { subjects = []; coverage = coverage with { Reason = "coverage-unverified", NextAction = "verify-and-replay-sources" }; }
                if (currentPolicy != await policy.ReadAsync(tenant, clock, cancellationToken).ConfigureAwait(false))
                { currentPolicy = null; }
            }
            catch (Exception ex) when (ex is IOException or InvalidDataException or HttpRequestException or Dapr.DaprException or Grpc.Core.RpcException)
            { subjects = []; coverage = coverage with { Reason = "projection-incomplete" }; }
        }

        if (currentPolicy is { } rule && !AdminQueueSummaryProjector.EligibleBuckets(scope, subjects.Select(s => s.ToItem(clock.UtcNow)), rule.Minimum).Any())
        { coverage = coverage with { Reason = "coverage-unverified", NextAction = "verify-and-replay-sources" }; }
        return (subjects.Select(subject => subject.ToItem(clock.UtcNow)).ToArray(), currentPolicy, coverage);
    }
}
