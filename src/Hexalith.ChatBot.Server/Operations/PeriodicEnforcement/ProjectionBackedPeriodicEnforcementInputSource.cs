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
    ISystemClock clock) : IPeriodicEnforcementInputSource
{
    public async ValueTask<IReadOnlyList<string>> GetTenantRefsAsync(CancellationToken cancellationToken)
        => await conversationStore.EnumerateTenantIdsAsync(cancellationToken).ConfigureAwait(false);

    public async ValueTask<PeriodicEnforcementTenantInputs> GetTenantInputsAsync(
        string tenantRef,
        CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(tenantRef);

        IReadOnlyList<AdminQueueSummaryProjectionItem> queueItems = await conversationStore
            .ReadOperationalQueueItemsAsync(tenantRef, clock.UtcNow, cancellationToken)
            .ConfigureAwait(false);
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
