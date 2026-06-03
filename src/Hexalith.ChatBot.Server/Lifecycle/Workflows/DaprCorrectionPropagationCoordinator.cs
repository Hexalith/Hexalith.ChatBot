using Hexalith.ChatBot.Server.Association;
using Hexalith.ChatBot.Server.Audit;
using Hexalith.ChatBot.Server.Gateway.Stages;

namespace Hexalith.ChatBot.Server.Lifecycle.Workflows;

internal sealed class DaprCorrectionPropagationCoordinator(
    ICorrectionPropagationCommandWriter writer,
    IEnumerable<ICorrectionPropagationStoreActivity> activities,
    IOperatorAlertSink operatorAlertSink,
    IAuditWriter auditWriter,
    ISystemClock clock) : ICorrectionPropagationCoordinator
{
    public const string SchemaVersion = "chatbot.association-correction-propagation.v1";
    public const string ResponsibleOwnerRole = "operations";
    public const string PendingNextSafeAction = "wait-for-propagation";
    public const string DelayedNextSafeAction = "escalate-to-operations";
    public const string DefaultDelayReasonCode = "m0_store_invalidation_failed";
    public static readonly TimeSpan M0M1P95Target = TimeSpan.FromMinutes(10);

    private readonly IReadOnlyDictionary<string, ICorrectionPropagationStoreActivity> _activities =
        activities.ToDictionary(static activity => activity.StoreKey, StringComparer.Ordinal);

    // Story 9.6 (AC1): the scope is M2 (the four M0 stores PLUS vector-reindex) only when the vector-reindex activity is
    // registered; otherwise the existing M0 scope holds, so an M0 deployment behaves exactly as Story 2.8 did.
    private IReadOnlyList<string> Scope => _activities.ContainsKey(CorrectionPropagationStoreKeys.VectorReindex)
        ? CorrectionPropagationStoreKeys.RequiredM2
        : CorrectionPropagationStoreKeys.RequiredM0;

    private CorrectionPropagationScope SloScope => _activities.ContainsKey(CorrectionPropagationStoreKeys.VectorReindex)
        ? CorrectionPropagationScope.M2
        : CorrectionPropagationScope.M0M1;

    public bool IsReady => Scope.All(_activities.ContainsKey);

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
                Scope,
                request.SourceVersion,
                request.StartedAtUtc,
                request.EstimatedCompletionAtUtc,
                ResponsibleOwnerRole,
                PendingNextSafeAction,
                SchemaVersion),
            cancellationToken)
            .ConfigureAwait(false);

        List<CorrectionPropagationActivityResult> results = [];
        foreach (string storeKey in Scope)
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

        // The delay reason is the first failing store's reason code (e.g. vector_reindex_slo_exceeded /
        // vector_reindex_failed), falling back to the generic M0 store-invalidation reason for the M0 failure path.
        string delayReason = results
            .FirstOrDefault(static result => !result.IsSuccessful)?.FailureReasonCode
            ?? DefaultDelayReasonCode;
        await MarkDelayedAsync(request, delayReason, cancellationToken).ConfigureAwait(false);
    }

    private async ValueTask MarkDelayedAsync(
        CorrectionPropagationRequest request,
        string reasonCode,
        CancellationToken cancellationToken)
    {
        DateTimeOffset now = clock.UtcNow;

        await writer.SubmitAsync(
            request,
            nameof(DelayMailboxAssociationCorrectionPropagation),
            new DelayMailboxAssociationCorrectionPropagation(
                request.AssociationId,
                request.CorrectionId,
                request.WorkflowInstanceId,
                request.SourceVersion,
                now,
                ResponsibleOwnerRole,
                DelayedNextSafeAction,
                reasonCode,
                SchemaVersion),
            cancellationToken)
            .ConfigureAwait(false);

        // Story 9.6 (AC2): fail-closed audit-then-deliver. Write the metadata-only P2 audit envelope BEFORE emitting the
        // single CorrectionDelayed alert; if the audit write fails, suppress the alert (never the reverse).
        AuditEnvelope envelope = AuditEnvelopeFactory.CorrectionPropagationDelayed(
            request.TenantId,
            request.AssociationId,
            request.CorrectionId,
            reasonCode,
            ResponsibleOwnerRole,
            DelayedNextSafeAction,
            request.CorrelationId,
            now);
        AuditWriteResult auditResult = await auditWriter
            .RecordPreCommitAsync(envelope, cancellationToken)
            .ConfigureAwait(false);
        if (!auditResult.Succeeded)
        {
            return;
        }

        await operatorAlertSink
            .EmitAsync(
                new OperatorAlert(
                    OperatorAlertKind.CorrectionDelayed,
                    reasonCode,
                    request.TenantId,
                    nameof(DelayMailboxAssociationCorrectionPropagation),
                    request.CorrelationId,
                    now,
                    P2IncidentLocator(reasonCode)),
                cancellationToken)
            .ConfigureAwait(false);
    }

    // A safe, bounded P2 incident locator token carried on the delay alert's FirstBreakLocator (metadata-only).
    private static string P2IncidentLocator(string reasonCode)
        => $"correction-propagation-p2:{AuditMetadata.SafeOptionalToken(reasonCode) ?? "correction_delayed"}";

    public static string CorrectionIdFor(string associationId, long sourceVersion)
        => $"{associationId}:correction:{sourceVersion.ToString(System.Globalization.CultureInfo.InvariantCulture)}";

    public static string WorkflowInstanceIdFor(string tenantId, string associationId, string correctionId, long sourceVersion)
        => $"{tenantId}:chatbot:correction-propagation:{associationId}:{correctionId}:{sourceVersion.ToString(System.Globalization.CultureInfo.InvariantCulture)}";
}
