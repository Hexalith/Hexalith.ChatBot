using Hexalith.ChatBot.Server.Association;
using Hexalith.ChatBot.Server.Gateway.Stages;
using Hexalith.ChatBot.Server.Observability;

namespace Hexalith.ChatBot.Server.Lifecycle.Workflows;

internal sealed class DaprCorrectionPropagationCoordinator(
    ICorrectionPropagationWorkflowRuntime runtime,
    ICorrectionPropagationActivityCatalog activityCatalog,
    IChatBotMetrics? metrics = null,
    ICorrectionPropagationWorkflowStatusSink? statusSink = null) : ICorrectionPropagationCoordinator
{
    public const string SchemaVersion = "chatbot.association-correction-propagation.v1";
    public const string ResponsibleOwnerRole = "operations";
    public const string PendingNextSafeAction = "wait-for-propagation";
    public const string DelayedNextSafeAction = "escalate-to-operations";
    public const string DefaultDelayReasonCode = "m0_store_invalidation_failed";
    public static readonly TimeSpan M0M1P95Target = TimeSpan.FromMinutes(10);

    private readonly IChatBotMetrics _metrics = metrics ?? NullChatBotMetrics.Instance;
    private readonly ICorrectionPropagationWorkflowStatusSink _statusSink =
        statusSink ?? NullCorrectionPropagationWorkflowStatusSink.Instance;

    public bool IsReady => runtime.IsAvailable && activityCatalog.IsReady;

    public async ValueTask StartAsync(CorrectionPropagationRequest request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (!IsReady)
        {
            _metrics.RecordWorkflowLifecycle(
                request.TenantId,
                CorrectionPropagationWorkflowStatuses.RuntimeUnavailable,
                CorrectionPropagationWorkflowFailureCodes.WorkflowUnavailable);
            throw new InvalidOperationException(CorrectionPropagationWorkflowFailureCodes.WorkflowUnavailable);
        }

        CorrectionPropagationRequest scheduledRequest = request with
        {
            EstimatedCompletionAtUtc = CorrectionPropagationSlo.DeadlineFor(activityCatalog.SloScope, request.StartedAtUtc),
        };
        try
        {
            await runtime.ScheduleAsync(scheduledRequest, cancellationToken).ConfigureAwait(false);
            _metrics.RecordWorkflowLifecycle(
                request.TenantId,
                CorrectionPropagationWorkflowStatuses.Started,
                CorrectionPropagationWorkflowFailureCodes.None);
            await _statusSink
                .ReportAsync(
                    scheduledRequest,
                    CorrectionPropagationWorkflowStatuses.Started,
                    workflowRetryCount: 0,
                    CorrectionPropagationWorkflowFailureCodes.None,
                    cancellationToken)
                .ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex) when (ex is not OutOfMemoryException and not StackOverflowException)
        {
            _metrics.RecordWorkflowLifecycle(
                request.TenantId,
                CorrectionPropagationWorkflowStatuses.RuntimeUnavailable,
                CorrectionPropagationWorkflowFailureCodes.WorkflowUnavailable);
            throw new InvalidOperationException(CorrectionPropagationWorkflowFailureCodes.WorkflowUnavailable, ex);
        }
    }

    public static string CorrectionIdFor(string associationId, long sourceVersion)
        => $"{associationId}:correction:{sourceVersion.ToString(System.Globalization.CultureInfo.InvariantCulture)}";

    public static string WorkflowInstanceIdFor(string tenantId, string associationId, string correctionId, long sourceVersion)
        => $"{tenantId}:chatbot:correction-propagation:{associationId}:{correctionId}:{sourceVersion.ToString(System.Globalization.CultureInfo.InvariantCulture)}";
}
