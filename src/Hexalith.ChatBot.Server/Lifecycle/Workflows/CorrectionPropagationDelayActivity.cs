using Dapr.Workflow;

using Hexalith.ChatBot.Server.Association;
using Hexalith.ChatBot.Server.Audit;
using Hexalith.ChatBot.Server.Gateway.Stages;
using Hexalith.ChatBot.Server.Observability;

namespace Hexalith.ChatBot.Server.Lifecycle.Workflows;

internal sealed class CorrectionPropagationDelayActivity(
    ICorrectionPropagationCommandWriter writer,
    IOperatorAlertSink operatorAlertSink,
    IAuditWriter auditWriter,
    ISystemClock clock,
    IChatBotMetrics? metrics = null,
    ICorrectionPropagationWorkflowStatusSink? statusSink = null)
    : WorkflowActivity<CorrectionPropagationDelayInput, bool>
{
    private readonly IChatBotMetrics _metrics = metrics ?? NullChatBotMetrics.Instance;
    private readonly ICorrectionPropagationWorkflowStatusSink _statusSink =
        statusSink ?? NullCorrectionPropagationWorkflowStatusSink.Instance;

    public override async Task<bool> RunAsync(
        WorkflowActivityContext context,
        CorrectionPropagationDelayInput input)
    {
        ArgumentNullException.ThrowIfNull(input);
        ArgumentNullException.ThrowIfNull(input.Request);
        CorrectionPropagationRequest request = input.Request;
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
                DaprCorrectionPropagationCoordinator.ResponsibleOwnerRole,
                DaprCorrectionPropagationCoordinator.DelayedNextSafeAction,
                input.ReasonCode,
                DaprCorrectionPropagationCoordinator.SchemaVersion),
            CancellationToken.None)
            .ConfigureAwait(false);

        AuditEnvelope envelope = AuditEnvelopeFactory.CorrectionPropagationDelayed(
            request.TenantId,
            request.AssociationId,
            request.CorrectionId,
            input.ReasonCode,
            DaprCorrectionPropagationCoordinator.ResponsibleOwnerRole,
            DaprCorrectionPropagationCoordinator.DelayedNextSafeAction,
            request.CorrelationId,
            now);
        AuditWriteResult auditResult = await auditWriter
            .RecordPreCommitAsync(envelope, CancellationToken.None)
            .ConfigureAwait(false);
        if (!auditResult.Succeeded)
        {
            _metrics.RecordWorkflowLifecycle(
                request.TenantId,
                CorrectionPropagationWorkflowStatuses.Delayed,
                CorrectionPropagationWorkflowFailureCodes.AuditUnavailable);
            await _statusSink
                .ReportAsync(
                    request,
                    CorrectionPropagationWorkflowStatuses.Delayed,
                    workflowRetryCount: 0,
                    CorrectionPropagationWorkflowFailureCodes.AuditUnavailable,
                    CancellationToken.None)
                .ConfigureAwait(false);
            return false;
        }

        await operatorAlertSink
            .EmitAsync(
                new OperatorAlert(
                    OperatorAlertKind.CorrectionDelayed,
                    input.ReasonCode,
                    request.TenantId,
                    nameof(DelayMailboxAssociationCorrectionPropagation),
                    request.CorrelationId,
                    now,
                    P2IncidentLocator(input.ReasonCode)),
                CancellationToken.None)
            .ConfigureAwait(false);
        _metrics.RecordWorkflowLifecycle(
            request.TenantId,
            CorrectionPropagationWorkflowStatuses.Delayed,
            input.ReasonCode);
        await _statusSink
            .ReportAsync(
                request,
                CorrectionPropagationWorkflowStatuses.Delayed,
                workflowRetryCount: 0,
                input.ReasonCode,
                CancellationToken.None)
            .ConfigureAwait(false);
        return true;
    }

    private static string P2IncidentLocator(string reasonCode)
        => $"correction-propagation-p2:{AuditMetadata.SafeOptionalToken(reasonCode) ?? "correction_delayed"}";
}
