using System.Collections.Concurrent;

using Hexalith.ChatBot.Contracts.Commands;
using Hexalith.ChatBot.Contracts.Enums;
using Hexalith.ChatBot.Contracts.Queries;
using Hexalith.ChatBot.Server.Audit;
using Hexalith.ChatBot.Server.Notifications;
using Hexalith.ChatBot.Server.Observability;
using Hexalith.ChatBot.Server.Projections;

using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace Hexalith.ChatBot.Server.Operations.PeriodicEnforcement;

internal sealed class ProjectionBackedPeriodicEnforcementInputSource(
    IProjectConversationProjectionStore conversationStore,
    ISystemClock clock, ReviewQueueStore? queues = null, IServiceScopeFactory? scopes = null, IChatBotMetrics? metrics = null) : IPeriodicEnforcementInputSource
{
    public async ValueTask<IReadOnlyList<string>> GetTenantRefsAsync(CancellationToken cancellationToken)
    {
        var tenants = await conversationStore.EnumerateTenantIdsAsync(cancellationToken).ConfigureAwait(false);
        return queues is null ? tenants : tenants.Concat(await queues.ReadTenantsAsync(cancellationToken).ConfigureAwait(false))
            .Distinct(StringComparer.Ordinal).ToArray();
    }

    public async ValueTask<PeriodicEnforcementTenantInputs> GetTenantInputsAsync(
        string tenantRef,
        CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(tenantRef);

        IReadOnlyList<AdminQueueSummaryProjectionItem> queueItems;
        if (queues is null)
        {
            queueItems = await conversationStore.ReadOperationalQueueItemsAsync(tenantRef, clock.UtcNow, cancellationToken).ConfigureAwait(false);
        }
        else
        {
            using var scope = scopes?.CreateScope();
            var observations = scope?.ServiceProvider.GetService<ReviewQueueAggregateReader>();
            List<AdminQueueSummaryProjectionItem> work = [];
            foreach (var family in OperationalQueueFamilies.All)
            {
                var subjects = await queues.ReadFamilyAsync(tenantRef, family, cancellationToken).ConfigureAwait(false);
                work.AddRange(subjects.Where(subject => !subject.Current.Terminal).Select(subject => subject.ToItem(clock.UtcNow)));
                if (observations is not null && metrics is not null)
                {
                    var observation = await observations.ReadAsync(tenantRef, family, cancellationToken).ConfigureAwait(false);
                    foreach (var bucket in AdminQueueSummaryProjector.EligibleBuckets(observation.Coverage.Scope, observation.Items, observation.Policy?.Minimum))
                    { metrics.RecordReviewQueue(tenantRef, family, bucket.Status, bucket.Count, bucket.OldestAgeSeconds); }
                }
            }
            queueItems = work;
        }
        IReadOnlyList<ApprovalEventView> approvals = await conversationStore
            .ReadApprovalEventsAsync(tenantRef, cancellationToken)
            .ConfigureAwait(false);

        OperationalQueueDiagnostics[] diagnostics = queueItems
            .Select(static item => AdminQueueSummaryProjector.Search(
                new SearchOperationalQueueItems(
                    item.QueueFamily,
                    PageSize: 1,
                    PageToken: null,
                    OperationalQueueSortKey.Age,
                    SortDescending: true,
                    new OperationalQueueFilter()),
                [item],
                item.CorrelationId ?? "periodic-enforcement").Rows.FirstOrDefault()?.Diagnostics)
            .OfType<OperationalQueueDiagnostics>()
            .ToArray();

        return new PeriodicEnforcementTenantInputs(
            queueItems,
            RecipientCandidates: [],
            new EscalationPolicyChangeSet([]),
            NotificationDeliveries: [],
            NotificationThrottleCeilings.SafeDefaults,
            ReviewerBacklogThreshold.SafeDefault,
            BuildApprovalDecisionSamples(approvals),
            diagnostics);
    }

    private static IReadOnlyList<ApprovalDecisionSample> BuildApprovalDecisionSamples(IReadOnlyList<ApprovalEventView> approvals)
        => approvals
            .Where(static approval => approval.EventKind is ApprovalEventKind.Decision &&
                approval.DecisionKind is not null &&
                approval.RequestedAtUtc is not null &&
                approval.DecidedAtUtc is not null &&
                approval.AiRiskClass is not null)
            .Select(static approval => new ApprovalDecisionSample(
                approval.TenantId,
                approval.DecisionActorId,
                approval.RequestedAtUtc!.Value,
                approval.DecidedAtUtc!.Value,
                approval.DecisionKind!.Value,
                approval.AiRiskClass!.Value))
            .ToArray();
}
