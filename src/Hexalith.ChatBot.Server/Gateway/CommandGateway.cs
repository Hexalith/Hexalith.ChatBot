using Hexalith.ChatBot.Client.Generated;
using Hexalith.ChatBot.Server.Audit;
using Hexalith.ChatBot.Server.Gateway.Idempotency;
using Hexalith.ChatBot.Server.Gateway.Status;
using Hexalith.ChatBot.Server.Gateway.Stages;
using Hexalith.ChatBot.Server.Lifecycle.StateModel;
using Hexalith.ChatBot.Server.Observability;
using Hexalith.EventStore.Client.Gateway;

namespace Hexalith.ChatBot.Server.Gateway;

internal sealed class CommandGateway(
    ChatBotCommandAdmissionPipeline admission,
    IIdempotencyStore idempotencyStore,
    IAuditWriter auditWriter,
    IAuditReplayIntentQueue replayIntentQueue,
    IOperatorAlertSink operatorAlertSink,
    IOperationStatusStore operationStatusStore,
    ISystemClock clock,
    ICommandDispatcher dispatcher,
    IChatBotProblemDetailsFactory problemDetailsFactory)
{
    public CommandGateway(
        IAuthenticationStage authentication,
        ITenantBindingStage tenantBinding,
        IAuthorizationStage authorization,
        IRiskClassifier riskClassifier,
        IApprovalGate approvalGate,
        IIdempotencyStore idempotencyStore,
        IAuditWriter auditWriter,
        IAuditReplayIntentQueue replayIntentQueue,
        IOperatorAlertSink operatorAlertSink,
        IOperationStatusStore operationStatusStore,
        ISystemClock clock,
        ILifecycleTransitionGuard lifecycleTransitionGuard,
        ICommandDispatcher dispatcher,
        IChatBotProblemDetailsFactory problemDetailsFactory,
        ISpineCommandAllowlist commandAllowlist,
        IChatBotMetrics? metrics = null,
        IAuthorizationFailureCounter? authorizationFailureCounter = null)
        : this(
            new ChatBotCommandAdmissionPipeline(
                authentication,
                tenantBinding,
                authorization,
                riskClassifier,
                approvalGate,
                idempotencyStore,
                auditWriter,
                replayIntentQueue,
                operatorAlertSink,
                operationStatusStore,
                clock,
                lifecycleTransitionGuard,
                commandAllowlist,
                metrics,
                authorizationFailureCounter),
            idempotencyStore,
            auditWriter,
            replayIntentQueue,
            operatorAlertSink,
            operationStatusStore,
            clock,
            dispatcher,
            problemDetailsFactory)
    {
    }

    public async ValueTask<ChatBotGatewayResult> SubmitAsync(ChatBotCommandSubmission submission, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(submission);

        ChatBotCommandAdmissionDecision admissionDecision;
        try
        {
            admissionDecision = await admission.AdmitAsync(submission, cancellationToken).ConfigureAwait(false);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            return ChatBotGatewayResult.Denied(problemDetailsFactory.CreateAuditUnavailable(submission.CorrelationId, submission.TaskId));
        }

        if (admissionDecision.Kind == ChatBotCommandAdmissionDecisionKind.ReplayPriorOutcome)
        {
            CommandSubmissionResponse stored = admissionDecision.PriorOutcome!;
            stored.OperationId = string.IsNullOrWhiteSpace(stored.OperationId) ? stored.TaskId ?? stored.CommandId : stored.OperationId;
            stored.AcceptedAt = stored.AcceptedAt.ToUniversalTime();
            stored.PriorOutcome = new PriorCommandOutcome
            {
                CommandId = stored.CommandId,
                CorrelationId = stored.CorrelationId,
                OperationId = stored.OperationId,
                TaskId = stored.TaskId,
                LifecycleState = stored.LifecycleState,
                AcceptedAt = stored.AcceptedAt,
                ReasonCode = stored.ReasonCode,
                RetryEligible = stored.RetryEligible,
            };
            return ChatBotGatewayResult.AcceptedResult(stored);
        }

        if (!admissionDecision.IsAccepted)
        {
            return Denied(admissionDecision);
        }

        ChatBotGatewayContext context = admissionDecision.Context!;
        CoarseIdempotencyMetadata idempotency = admissionDecision.Idempotency!;
        LifecycleTransitionDefinition transition = admissionDecision.LifecycleTransition!;
        ChatBotDispatchResult dispatchResult;
        try
        {
            dispatchResult = await dispatcher.DispatchAsync(context, cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex) when (ex is EventStoreGatewayException or HttpRequestException or InvalidOperationException)
        {
            await idempotencyStore
                .AbortAdmissionAsync(idempotency, cancellationToken)
                .ConfigureAwait(false);
            AuditEnvelope preCommitEnvelope = AuditEnvelopeFactory.PreCommit(context, transition, clock.UtcNow);
            await QueueReplayIntentAsync(
                AuditReplayIntentKind.PreCommitOperationReplay,
                preCommitEnvelope,
                "dispatch_unavailable",
                cancellationToken)
                .ConfigureAwait(false);
            await AlertAsync(OperatorAlertKind.AuditUnavailable, preCommitEnvelope, "dispatch_unavailable", cancellationToken)
                .ConfigureAwait(false);

            return ChatBotGatewayResult.Denied(
                problemDetailsFactory.CreateDispatchUnavailable(submission.CorrelationId, submission.TaskId));
        }

        CommandSubmissionResponse response = new()
        {
            CommandId = submission.Request.CommandId,
            CorrelationId = submission.CorrelationId,
            TaskId = submission.TaskId,
            OperationId = submission.TaskId ?? submission.Request.CommandId,
            LifecycleState = LifecycleState.Proposed,
            AcceptedAt = dispatchResult.AcceptedAt.ToUniversalTime(),
            ReasonCode = ChatBotMessageCode.Command_accepted,
            RetryEligible = false,
        };

        try
        {
            await idempotencyStore
                .RecordOutcomeAsync(idempotency, response, cancellationToken)
                .ConfigureAwait(false);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            AuditEnvelope failedReceiptEnvelope = AuditEnvelopeFactory.PostCommit(context, dispatchResult, transition, clock.UtcNow);
            try
            {
                // The EventStore command has committed. Persist the metadata-only outcome
                // evidence in the independent audit seam before returning a retryable error.
                _ = await auditWriter.RecordPostCommitAsync(failedReceiptEnvelope, cancellationToken).ConfigureAwait(false);
            }
            catch (Exception auditException) when (auditException is not OperationCanceledException)
            {
                // The replay intent and operator alert still carry the exact safe outcome.
            }

            await QueueReplayIntentAsync(
                AuditReplayIntentKind.PostCommitAuditReconciliation,
                failedReceiptEnvelope,
                "idempotency_outcome_unavailable",
                cancellationToken,
                response,
                idempotency).ConfigureAwait(false);
            await AlertAsync(
                OperatorAlertKind.PostCommitAuditReconciliationRequired,
                failedReceiptEnvelope,
                "idempotency_outcome_unavailable",
                cancellationToken).ConfigureAwait(false);
            return ChatBotGatewayResult.Denied(problemDetailsFactory.CreateAuditUnavailable(submission.CorrelationId, submission.TaskId));
        }

        AuditEnvelope postCommitEnvelope = AuditEnvelopeFactory.PostCommit(context, dispatchResult, transition, clock.UtcNow);
        AuditWriteResult postCommitAudit = await auditWriter.RecordPostCommitAsync(postCommitEnvelope, cancellationToken).ConfigureAwait(false);
        if (!postCommitAudit.Succeeded)
        {
            await QueueReplayIntentAsync(
                AuditReplayIntentKind.PostCommitAuditReconciliation,
                postCommitEnvelope,
                postCommitAudit.ReasonCode,
                cancellationToken)
                .ConfigureAwait(false);
            await AlertAsync(
                OperatorAlertKind.PostCommitAuditReconciliationRequired,
                postCommitEnvelope,
                postCommitAudit.ReasonCode,
                cancellationToken)
                .ConfigureAwait(false);
        }

        await operationStatusStore
            .UpsertAsync(
                OperationStatusRecord.Accepted(
                    context.TenantBinding.TenantId,
                    response,
                    !postCommitAudit.Succeeded,
                    clock.UtcNow,
                    idempotency.OperationClass),
                cancellationToken)
            .ConfigureAwait(false);

        return ChatBotGatewayResult.AcceptedResult(response, !postCommitAudit.Succeeded);
    }

    private ChatBotGatewayResult Denied(ChatBotCommandAdmissionDecision decision)
    {
        string reasonCode = decision.ReasonCode ?? ChatBotAuthorizationReasonCodes.AuthorizationDenied;
        if (string.Equals(reasonCode, AuditFailureReasonCodes.AuditUnavailable, StringComparison.Ordinal))
        {
            return ChatBotGatewayResult.Denied(problemDetailsFactory.CreateAuditUnavailable(decision.CorrelationId, decision.TaskId));
        }

        if (string.Equals(reasonCode, "idempotency_outcome_unavailable", StringComparison.Ordinal))
        {
            return ChatBotGatewayResult.Denied(problemDetailsFactory.CreateAuditUnavailable(decision.CorrelationId, decision.TaskId));
        }

        if (string.Equals(reasonCode, LifecycleTransitionReasonCodes.InvalidTransition, StringComparison.Ordinal))
        {
            return ChatBotGatewayResult.Denied(problemDetailsFactory.CreateInvalidLifecycleTransition(decision.CorrelationId, decision.TaskId));
        }

        if (reasonCode.StartsWith("idempotency_conflict_", StringComparison.Ordinal))
        {
            return ChatBotGatewayResult.Denied(problemDetailsFactory.CreateIdempotencyConflict(decision.CorrelationId, decision.TaskId, reasonCode));
        }

        return ChatBotGatewayResult.Denied(string.Equals(reasonCode, ChatBotAuthorizationReasonCodes.CommandNotAllowlisted, StringComparison.Ordinal)
            ? problemDetailsFactory.CreateCommandNotAllowlisted(decision.CorrelationId, decision.TaskId)
            : problemDetailsFactory.CreateAuthorizationProblem(reasonCode, decision.CorrelationId, decision.TaskId));
    }

    private async ValueTask QueueReplayIntentAsync(
        AuditReplayIntentKind kind,
        AuditEnvelope envelope,
        string reasonCode,
        CancellationToken cancellationToken,
        CommandSubmissionResponse? acceptedOutcome = null,
        CoarseIdempotencyMetadata? idempotency = null)
    {
        await replayIntentQueue
            .EnqueueAsync(
                new AuditReplayIntent(
                    kind,
                    envelope.TenantId,
                    envelope.ActorId,
                    envelope.CommandName,
                    envelope.ResourceId,
                    envelope.CorrelationId,
                    envelope.IdempotencyKey,
                    reasonCode,
                    clock.UtcNow,
                    acceptedOutcome,
                    idempotency?.CoarseKeyHash,
                    idempotency?.IdentityKeyHash),
                cancellationToken)
            .ConfigureAwait(false);
    }

    private async ValueTask AlertAsync(
        OperatorAlertKind kind,
        AuditEnvelope envelope,
        string reasonCode,
        CancellationToken cancellationToken)
    {
        await operatorAlertSink
            .EmitAsync(
                new OperatorAlert(
                    kind,
                    reasonCode,
                    envelope.TenantId,
                    envelope.CommandName,
                    envelope.CorrelationId,
                    clock.UtcNow),
                cancellationToken)
            .ConfigureAwait(false);
    }
}
