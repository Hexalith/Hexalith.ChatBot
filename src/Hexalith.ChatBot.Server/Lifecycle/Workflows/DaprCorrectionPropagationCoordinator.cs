using Hexalith.ChatBot.Server.Association;
using Hexalith.ChatBot.Server.Audit;

namespace Hexalith.ChatBot.Server.Lifecycle.Workflows;

internal sealed class DaprCorrectionPropagationCoordinator(
    ICorrectionPropagationCommandWriter writer,
    IEnumerable<ICorrectionPropagationStoreActivity> activities,
    IOperatorAlertSink operatorAlertSink,
    ISystemClock clock) : ICorrectionPropagationCoordinator
{
    public const string SchemaVersion = "chatbot.association-correction-propagation.v1";
    public const string ResponsibleOwnerRole = "operations";
    public const string PendingNextSafeAction = "wait-for-propagation";
    public const string DelayedNextSafeAction = "escalate-to-operations";
    public static readonly TimeSpan M0M1P95Target = TimeSpan.FromMinutes(10);

    private readonly IReadOnlyDictionary<string, ICorrectionPropagationStoreActivity> _activities =
        activities.ToDictionary(static activity => activity.StoreKey, StringComparer.Ordinal);

    public bool IsReady => CorrectionPropagationStoreKeys.RequiredM0.All(_activities.ContainsKey);

    public async ValueTask StartAsync(CorrectionPropagationRequest request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (!IsReady)
        {
            throw new InvalidOperationException("correction_propagation_dependency_unavailable");
        }

        await writer.SubmitAsync(
            request,
            nameof(StartMailboxAssociationCorrectionPropagation),
            new StartMailboxAssociationCorrectionPropagation(
                request.AssociationId,
                request.IntakeId,
                request.CorrectionId,
                request.WorkflowInstanceId,
                request.PriorProjectId,
                request.CorrectedProjectId,
                CorrectionPropagationStoreKeys.RequiredM0,
                request.SourceVersion,
                request.StartedAtUtc,
                request.EstimatedCompletionAtUtc,
                ResponsibleOwnerRole,
                PendingNextSafeAction,
                SchemaVersion),
            cancellationToken)
            .ConfigureAwait(false);

        List<CorrectionPropagationActivityResult> results = [];
        foreach (string storeKey in CorrectionPropagationStoreKeys.RequiredM0)
        {
            CorrectionPropagationActivityRequest activityRequest = new(
                request.TenantId,
                request.AssociationId,
                request.CorrectionId,
                request.WorkflowInstanceId,
                storeKey,
                request.SourceVersion,
                request.PriorProjectId,
                request.CorrectedProjectId,
                clock.UtcNow,
                request.CorrelationId);
            CorrectionPropagationActivityResult result = await _activities[storeKey]
                .InvalidateAndRebuildAsync(activityRequest, cancellationToken)
                .ConfigureAwait(false);
            results.Add(result);

            await writer.SubmitAsync(
                request,
                nameof(AcknowledgeMailboxAssociationCorrectionStoreInvalidated),
                new AcknowledgeMailboxAssociationCorrectionStoreInvalidated(
                    request.AssociationId,
                    request.CorrectionId,
                    request.WorkflowInstanceId,
                    storeKey,
                    request.SourceVersion,
                    request.PriorProjectId,
                    request.CorrectedProjectId,
                    activityRequest.StartedAtUtc,
                    result.CompletedAtUtc,
                    result.Outcome,
                    result.FailureReasonCode,
                    "metadata_only",
                    "collaboration_input",
                    SchemaVersion),
                cancellationToken)
                .ConfigureAwait(false);
        }

        if (results.All(static result => result.IsSuccessful))
        {
            await writer.SubmitAsync(
                request,
                nameof(CompleteMailboxAssociationCorrectionPropagation),
                new CompleteMailboxAssociationCorrectionPropagation(
                    request.AssociationId,
                    request.CorrectionId,
                    request.WorkflowInstanceId,
                    request.SourceVersion,
                    clock.UtcNow,
                    CorrectionPropagationStatuses.Complete,
                    SchemaVersion),
                cancellationToken)
                .ConfigureAwait(false);
            return;
        }

        await MarkDelayedAsync(request, "m0_store_invalidation_failed", cancellationToken).ConfigureAwait(false);
    }

    private async ValueTask MarkDelayedAsync(
        CorrectionPropagationRequest request,
        string reasonCode,
        CancellationToken cancellationToken)
    {
        await writer.SubmitAsync(
            request,
            nameof(DelayMailboxAssociationCorrectionPropagation),
            new DelayMailboxAssociationCorrectionPropagation(
                request.AssociationId,
                request.CorrectionId,
                request.WorkflowInstanceId,
                request.SourceVersion,
                clock.UtcNow,
                ResponsibleOwnerRole,
                DelayedNextSafeAction,
                reasonCode,
                SchemaVersion),
            cancellationToken)
            .ConfigureAwait(false);

        await operatorAlertSink
            .EmitAsync(
                new OperatorAlert(
                    OperatorAlertKind.CorrectionDelayed,
                    reasonCode,
                    request.TenantId,
                    nameof(DelayMailboxAssociationCorrectionPropagation),
                    request.CorrelationId,
                    clock.UtcNow),
                cancellationToken)
            .ConfigureAwait(false);
    }

    public static string CorrectionIdFor(string associationId, long sourceVersion)
        => $"{associationId}:correction:{sourceVersion.ToString(System.Globalization.CultureInfo.InvariantCulture)}";

    public static string WorkflowInstanceIdFor(string tenantId, string associationId, string correctionId, long sourceVersion)
        => $"{tenantId}:chatbot:correction-propagation:{associationId}:{correctionId}:{sourceVersion.ToString(System.Globalization.CultureInfo.InvariantCulture)}";
}
