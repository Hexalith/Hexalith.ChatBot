using Hexalith.ChatBot.Client.Generated;

using Hexalith.ChatBot.Server.Audit;
using Hexalith.ChatBot.Server.Gateway.Idempotency;
using Hexalith.ChatBot.Server.Gateway.Status;
using Hexalith.ChatBot.Server.Gateway.Stages;
using Hexalith.ChatBot.Server.Lifecycle.StateModel;
using Hexalith.EventStore.Client.Gateway;

namespace Hexalith.ChatBot.Server.Gateway;

internal sealed class CommandGateway(
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
    ISpineCommandAllowlist commandAllowlist)
{
    public async ValueTask<ChatBotGatewayResult> SubmitAsync(ChatBotCommandSubmission submission, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(submission);

        ChatBotAuthenticationResult authenticationResult = await authentication
            .AuthenticateAsync(submission, cancellationToken)
            .ConfigureAwait(false);
        if (!authenticationResult.IsAuthenticated)
        {
            return await DenyAsync(
                submission,
                "unavailable",
                "anonymous",
                authenticationResult.ReasonCode,
                cancellationToken)
                .ConfigureAwait(false);
        }

        ChatBotAuthenticatedActor actor = authenticationResult.Actor!;
        ChatBotTenantBindingResult bindingResult = await tenantBinding
            .BindTenantAsync(submission, actor, cancellationToken)
            .ConfigureAwait(false);
        if (!bindingResult.IsBound)
        {
            if (IsMailboxIntake(submission) &&
                string.Equals(bindingResult.ReasonCode, ChatBotAuthorizationReasonCodes.TenantMissing, StringComparison.Ordinal))
            {
                await QueueUnresolvedMailboxScopeAsync(submission, actor, bindingResult.ReasonCode, cancellationToken)
                    .ConfigureAwait(false);
            }

            return await DenyAsync(
                submission,
                bindingResult.Binding?.TenantId ?? "unavailable",
                actor.ActorId,
                bindingResult.ReasonCode,
                cancellationToken)
                .ConfigureAwait(false);
        }

        ChatBotTenantBinding binding = bindingResult.Binding!;
        ChatBotAuthorizationResult authorizationResult = await authorization
            .AuthorizeAsync(submission, actor, binding, cancellationToken)
            .ConfigureAwait(false);
        if (!authorizationResult.IsAllowed)
        {
            return await DenyAsync(
                submission,
                binding.TenantId,
                actor.ActorId,
                authorizationResult.ReasonCode,
                cancellationToken)
                .ConfigureAwait(false);
        }

        // Spine allowlist guardrail (fail-closed): a command type that is not on the hardcoded M0 spine
        // allowlist is rejected here — after authorization, before any durable-state work (no idempotency
        // admission, no dispatch) — with a catalog-backed, redacted problem and an authorization-failure fact.
        if (!commandAllowlist.IsAllowed(submission.Request.CommandType))
        {
            await auditWriter
                .RecordAuthorizationFailureAsync(
                    new ChatBotAuthorizationFailureAuditFact(
                        binding.TenantId,
                        actor.ActorId,
                        AuditMetadata.SafeCommandName(submission.Request.CommandType),
                        ChatBotAuthorizationReasonCodes.CommandNotAllowlisted,
                        submission.CorrelationId,
                        submission.TaskId,
                        Contracts.Enums.ChatBotSurfaceOrigins.ToWireValue(submission.Origin)),
                    cancellationToken)
                .ConfigureAwait(false);

            return ChatBotGatewayResult.Denied(
                problemDetailsFactory.CreateCommandNotAllowlisted(submission.CorrelationId, submission.TaskId));
        }

        ChatBotGatewayContext context = new(submission, actor, binding);
        ChatBotRiskClassification riskClassification = await riskClassifier.ClassifyAsync(context, cancellationToken).ConfigureAwait(false);
        if (riskClassification.Rejected)
        {
            return ChatBotGatewayResult.Denied(
                problemDetailsFactory.CreateCommandNotAllowlisted(submission.CorrelationId, submission.TaskId));
        }

        _ = await approvalGate.EvaluateAsync(context, cancellationToken).ConfigureAwait(false);
        CoarseIdempotencyDecision idempotencyDecision = await idempotencyStore
            .RecordAdmissionAsync(context, cancellationToken)
            .ConfigureAwait(false);
        if (idempotencyDecision.Kind == CoarseIdempotencyDecisionKind.ReplayPriorOutcome)
        {
            CommandSubmissionResponse priorOutcome = idempotencyDecision.PriorOutcome!;

            if (string.Equals(idempotencyDecision.Metadata.OperationClass, CoarseIdempotencyOperationClass.MessageIntake.Code, StringComparison.Ordinal))
            {
                _ = await auditWriter
                    .RecordPostCommitAsync(
                        AuditEnvelopeFactory.DuplicateMailboxIntakeSuppressed(context, clock.UtcNow),
                        cancellationToken)
                    .ConfigureAwait(false);
            }

            // A replay must resolve to the SAME operation-status record and must never downgrade a pending
            // post-commit reconciliation ('reconciling') to 'committed': the prior outcome carries no
            // reconciliation flag, so re-deriving it as false would falsely report audit as done. Preserve the
            // existing record (refreshing only LastUpdatedAt); fall back to a fresh record only if none exists.
            OperationStatusRecord? existingStatus = await operationStatusStore
                .TryGetAsync(binding.TenantId, OperationStatusRecord.OperationIdFor(priorOutcome), cancellationToken)
                .ConfigureAwait(false);
            OperationStatusRecord replayStatus = existingStatus is not null
                ? existingStatus with { LastUpdatedAt = clock.UtcNow }
                : OperationStatusRecord.Accepted(binding.TenantId, priorOutcome, false, clock.UtcNow);
            if (string.Equals(idempotencyDecision.Metadata.OperationClass, CoarseIdempotencyOperationClass.MessageIntake.Code, StringComparison.Ordinal))
            {
                replayStatus = replayStatus with
                {
                    OperationClass = CoarseIdempotencyOperationClass.MessageIntake.Code,
                    OriginalOperationId = OperationStatusRecord.OperationIdFor(priorOutcome),
                    DuplicateAttemptCount = replayStatus.DuplicateAttemptCount + 1,
                    DuplicateSafetyNote = "duplicate-provider-message-suppressed",
                    SafeNextActions = [Contracts.Messages.ChatBotMessageNextActions.None],
                    PartialOutputCodes = ["duplicate_suppressed"],
                };
            }

            await operationStatusStore
                .UpsertAsync(replayStatus, cancellationToken)
                .ConfigureAwait(false);

            return ChatBotGatewayResult.AcceptedResult(priorOutcome);
        }

        if (idempotencyDecision.Kind == CoarseIdempotencyDecisionKind.Conflict)
        {
            return ChatBotGatewayResult.Denied(
                problemDetailsFactory.CreateIdempotencyConflict(
                    submission.CorrelationId,
                    submission.TaskId,
                    CoarseIdempotencyOperationClass.ConflictCodeFor(idempotencyDecision.Metadata.OperationClass)));
        }

        LifecycleTransitionValidation lifecycleTransition = lifecycleTransitionGuard.ValidateCommandSubmission(context);
        if (!lifecycleTransition.IsValid)
        {
            AuditEnvelope rejectionEnvelope = AuditEnvelopeFactory.RejectedLifecycleTransition(context, lifecycleTransition, clock.UtcNow);
            AuditWriteResult rejectionAudit = await auditWriter.RecordPreCommitAsync(rejectionEnvelope, cancellationToken).ConfigureAwait(false);
            if (!rejectionAudit.Succeeded)
            {
                await QueueReplayIntentAsync(
                    AuditReplayIntentKind.PreCommitOperationReplay,
                    rejectionEnvelope,
                    rejectionAudit.ReasonCode,
                    cancellationToken)
                    .ConfigureAwait(false);
                await AlertAsync(OperatorAlertKind.AuditUnavailable, rejectionEnvelope, rejectionAudit.ReasonCode, cancellationToken)
                    .ConfigureAwait(false);

                await idempotencyStore
                    .AbortAdmissionAsync(idempotencyDecision.Metadata, cancellationToken)
                    .ConfigureAwait(false);

                return ChatBotGatewayResult.Denied(
                    problemDetailsFactory.CreateAuditUnavailable(submission.CorrelationId, submission.TaskId));
            }

            await idempotencyStore
                .AbortAdmissionAsync(idempotencyDecision.Metadata, cancellationToken)
                .ConfigureAwait(false);

            return ChatBotGatewayResult.Denied(
                problemDetailsFactory.CreateInvalidLifecycleTransition(submission.CorrelationId, submission.TaskId));
        }

        AuditEnvelope preCommitEnvelope = AuditEnvelopeFactory.PreCommit(context, lifecycleTransition.Transition, clock.UtcNow);
        AuditWriteResult preCommitAudit = await auditWriter.RecordPreCommitAsync(preCommitEnvelope, cancellationToken).ConfigureAwait(false);
        if (!preCommitAudit.Succeeded)
        {
            await QueueReplayIntentAsync(
                AuditReplayIntentKind.PreCommitOperationReplay,
                preCommitEnvelope,
                preCommitAudit.ReasonCode,
                cancellationToken)
                .ConfigureAwait(false);
            await AlertAsync(OperatorAlertKind.AuditUnavailable, preCommitEnvelope, preCommitAudit.ReasonCode, cancellationToken)
                .ConfigureAwait(false);

            await idempotencyStore
                .AbortAdmissionAsync(idempotencyDecision.Metadata, cancellationToken)
                .ConfigureAwait(false);

            return ChatBotGatewayResult.Denied(
                problemDetailsFactory.CreateAuditUnavailable(submission.CorrelationId, submission.TaskId));
        }

        ChatBotDispatchResult dispatchResult;
        try
        {
            dispatchResult = await dispatcher.DispatchAsync(context, cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex) when (ex is EventStoreGatewayException or HttpRequestException or InvalidOperationException)
        {
            // Fail-closed: the durable EventStore write could not complete (gateway unreachable / non-2xx, or an
            // unreadable command payload). Pre-commit audit already succeeded but no durable state exists, so we
            // must release the coarse-idempotency admission (InMemory: avoids a never-completing wait that would
            // poison a retry; Dapr: avoids a permanent Conflict until TTL), queue a replay intent, alert the
            // operator, and return a catalog-backed redacted problem — never let the exception escape as a raw
            // 500 leaking exception text (NFR40) or leave a dangling admission.
            await idempotencyStore
                .AbortAdmissionAsync(idempotencyDecision.Metadata, cancellationToken)
                .ConfigureAwait(false);
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
            LifecycleState = LifecycleState.Proposed,
            AcceptedAt = dispatchResult.AcceptedAt,
        };

        await idempotencyStore
            .RecordOutcomeAsync(idempotencyDecision.Metadata, response, cancellationToken)
            .ConfigureAwait(false);

        AuditEnvelope postCommitEnvelope = AuditEnvelopeFactory.PostCommit(context, dispatchResult, lifecycleTransition.Transition, clock.UtcNow);
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
                    binding.TenantId,
                    response,
                    !postCommitAudit.Succeeded,
                    clock.UtcNow,
                    idempotencyDecision.Metadata.OperationClass),
                cancellationToken)
            .ConfigureAwait(false);

        return ChatBotGatewayResult.AcceptedResult(response, !postCommitAudit.Succeeded);
    }

    private async ValueTask QueueReplayIntentAsync(
        AuditReplayIntentKind kind,
        AuditEnvelope envelope,
        string reasonCode,
        CancellationToken cancellationToken)
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
                    clock.UtcNow),
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

    private async ValueTask QueueUnresolvedMailboxScopeAsync(
        ChatBotCommandSubmission submission,
        ChatBotAuthenticatedActor actor,
        string reasonCode,
        CancellationToken cancellationToken)
    {
        string commandName = AuditMetadata.SafeCommandName(submission.Request.CommandType);
        const string unresolvedTenant = "unresolved";

        await replayIntentQueue
            .EnqueueAsync(
                new AuditReplayIntent(
                    AuditReplayIntentKind.PreCommitOperationReplay,
                    unresolvedTenant,
                    actor.ActorId,
                    commandName,
                    submission.Request.CommandId,
                    submission.CorrelationId,
                    IdempotencyKey: null,
                    reasonCode,
                    clock.UtcNow),
                cancellationToken)
            .ConfigureAwait(false);

        await operatorAlertSink
            .EmitAsync(
                new OperatorAlert(
                    OperatorAlertKind.TenantScopeUnresolved,
                    reasonCode,
                    unresolvedTenant,
                    commandName,
                    submission.CorrelationId,
                    clock.UtcNow),
                cancellationToken)
            .ConfigureAwait(false);
    }

    private async ValueTask<ChatBotGatewayResult> DenyAsync(
        ChatBotCommandSubmission submission,
        string tenantId,
        string actorId,
        string reasonCode,
        CancellationToken cancellationToken)
    {
        await auditWriter
            .RecordAuthorizationFailureAsync(
                new ChatBotAuthorizationFailureAuditFact(
                    tenantId,
                    actorId,
                    AuditMetadata.SafeCommandName(submission.Request.CommandType),
                    reasonCode,
                    submission.CorrelationId,
                    submission.TaskId,
                    Contracts.Enums.ChatBotSurfaceOrigins.ToWireValue(submission.Origin)),
                cancellationToken)
            .ConfigureAwait(false);

        return ChatBotGatewayResult.Denied(
            problemDetailsFactory.CreateAuthorizationProblem(reasonCode, submission.CorrelationId, submission.TaskId));
    }

    private static bool IsMailboxIntake(ChatBotCommandSubmission submission)
        => string.Equals(
            submission.Request.CommandType,
            nameof(Hexalith.ChatBot.Contracts.Commands.CaptureMailboxMessageIntake),
            StringComparison.Ordinal);
}
