using Hexalith.ChatBot.Client.Generated;
using Hexalith.ChatBot.Contracts.Enums;
using Hexalith.ChatBot.Server.Audit;
using Hexalith.ChatBot.Server.Gateway.Idempotency;
using Hexalith.ChatBot.Server.Gateway.Status;
using Hexalith.ChatBot.Server.Gateway.Stages;
using Hexalith.ChatBot.Server.Lifecycle.StateModel;
using Hexalith.ChatBot.Server.Observability;

namespace Hexalith.ChatBot.Server.Gateway;

internal sealed class ChatBotCommandAdmissionPipeline(
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
    ISpineCommandAllowlist commandAllowlist,
    IChatBotMetrics? metrics = null,
    IAuthorizationFailureCounter? authorizationFailureCounter = null)
{
    private readonly IChatBotMetrics _metrics = metrics ?? NullChatBotMetrics.Instance;

    public async ValueTask<ChatBotCommandAdmissionDecision> AdmitAsync(
        ChatBotCommandSubmission submission,
        CancellationToken cancellationToken)
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
                        ChatBotSurfaceOrigins.ToWireValue(submission.Origin)),
                    cancellationToken)
                .ConfigureAwait(false);

            authorizationFailureCounter?.Record(binding.TenantId, clock.UtcNow);

            return ChatBotCommandAdmissionDecision.Rejected(
                ChatBotAuthorizationReasonCodes.CommandNotAllowlisted,
                submission.CorrelationId,
                submission.TaskId);
        }

        ChatBotGatewayContext context = new(submission, actor, binding, authorizationResult.ServiceClientGrantEvidence);
        ChatBotRiskClassification riskClassification = await riskClassifier.ClassifyAsync(context, cancellationToken).ConfigureAwait(false);
        context.SetRiskClassification(riskClassification);
        if (riskClassification.Rejected)
        {
            return ChatBotCommandAdmissionDecision.Rejected(
                ChatBotAuthorizationReasonCodes.CommandNotAllowlisted,
                submission.CorrelationId,
                submission.TaskId);
        }

        ChatBotApprovalResult approvalResult = await approvalGate.EvaluateAsync(context, cancellationToken).ConfigureAwait(false);
        context.SetApprovalResult(approvalResult);
        if (approvalResult.Kind is ChatBotApprovalResultKind.Blocked)
        {
            return ChatBotCommandAdmissionDecision.Rejected(
                ChatBotAuthorizationReasonCodes.CommandNotAllowlisted,
                submission.CorrelationId,
                submission.TaskId);
        }

        CoarseIdempotencyDecision idempotencyDecision = await idempotencyStore
            .RecordAdmissionAsync(context, cancellationToken)
            .ConfigureAwait(false);
        if (idempotencyDecision.Kind == CoarseIdempotencyDecisionKind.ReplayPriorOutcome)
        {
            await RecordDuplicateReplaySideEffectsAsync(context, idempotencyDecision, cancellationToken)
                .ConfigureAwait(false);
            return ChatBotCommandAdmissionDecision.ReplayPriorOutcome(idempotencyDecision.PriorOutcome!);
        }

        if (idempotencyDecision.Kind == CoarseIdempotencyDecisionKind.Conflict)
        {
            return ChatBotCommandAdmissionDecision.Rejected(
                CoarseIdempotencyOperationClass.ConflictCodeFor(idempotencyDecision.Metadata.OperationClass),
                submission.CorrelationId,
                submission.TaskId);
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

                return ChatBotCommandAdmissionDecision.Rejected(
                    AuditFailureReasonCodes.AuditUnavailable,
                    submission.CorrelationId,
                    submission.TaskId);
            }

            await idempotencyStore
                .AbortAdmissionAsync(idempotencyDecision.Metadata, cancellationToken)
                .ConfigureAwait(false);

            return ChatBotCommandAdmissionDecision.Rejected(
                LifecycleTransitionReasonCodes.InvalidTransition,
                submission.CorrelationId,
                submission.TaskId);
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

            return ChatBotCommandAdmissionDecision.Rejected(
                AuditFailureReasonCodes.AuditUnavailable,
                submission.CorrelationId,
                submission.TaskId);
        }

        return ChatBotCommandAdmissionDecision.Accepted(context, idempotencyDecision.Metadata, lifecycleTransition.Transition);
    }

    private async ValueTask RecordDuplicateReplaySideEffectsAsync(
        ChatBotGatewayContext context,
        CoarseIdempotencyDecision idempotencyDecision,
        CancellationToken cancellationToken)
    {
        CommandSubmissionResponse priorOutcome = idempotencyDecision.PriorOutcome!;

        if (string.Equals(idempotencyDecision.Metadata.OperationClass, CoarseIdempotencyOperationClass.MessageIntake.Code, StringComparison.Ordinal))
        {
            LifecycleTransitionValidation skipTransition = lifecycleTransitionGuard
                .ResolveSkipTransition(LifecycleSkipTrigger.DuplicateSuppression);
            _ = await auditWriter
                .RecordPostCommitAsync(
                    AuditEnvelopeFactory.DuplicateMailboxIntakeSuppressed(context, skipTransition.Transition, clock.UtcNow),
                    cancellationToken)
                .ConfigureAwait(false);

            _metrics.RecordDuplicateSuppressed(context.TenantBinding.TenantId);
        }

        OperationStatusRecord? existingStatus = await operationStatusStore
            .TryGetAsync(context.TenantBinding.TenantId, OperationStatusRecord.OperationIdFor(priorOutcome), cancellationToken)
            .ConfigureAwait(false);
        OperationStatusRecord replayStatus = existingStatus is not null
            ? existingStatus with { LastUpdatedAt = clock.UtcNow }
            : OperationStatusRecord.Accepted(context.TenantBinding.TenantId, priorOutcome, false, clock.UtcNow);
        if (string.Equals(idempotencyDecision.Metadata.OperationClass, CoarseIdempotencyOperationClass.MessageIntake.Code, StringComparison.Ordinal))
        {
            replayStatus = replayStatus with
            {
                OperationClass = CoarseIdempotencyOperationClass.MessageIntake.Code,
                OriginalOperationId = OperationStatusRecord.OperationIdFor(priorOutcome),
                DuplicateAttemptCount = replayStatus.DuplicateAttemptCount + 1,
                DuplicateSafetyNote = "duplicate-provider-message-suppressed",
                SafeNextActions = [Hexalith.ChatBot.Contracts.Messages.ChatBotMessageNextActions.None],
                PartialOutputCodes = ["duplicate_suppressed"],
            };
        }

        await operationStatusStore
            .UpsertAsync(replayStatus, cancellationToken)
            .ConfigureAwait(false);
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

    private async ValueTask<ChatBotCommandAdmissionDecision> DenyAsync(
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
                    ChatBotSurfaceOrigins.ToWireValue(submission.Origin)),
                cancellationToken)
            .ConfigureAwait(false);

        authorizationFailureCounter?.Record(tenantId, clock.UtcNow);

        return ChatBotCommandAdmissionDecision.Rejected(reasonCode, submission.CorrelationId, submission.TaskId);
    }

    private static bool IsMailboxIntake(ChatBotCommandSubmission submission)
        => string.Equals(
            submission.Request.CommandType,
            nameof(Contracts.Commands.CaptureMailboxMessageIntake),
            StringComparison.Ordinal);
}

internal sealed record ChatBotCommandAdmissionDecision(
    ChatBotCommandAdmissionDecisionKind Kind,
    ChatBotGatewayContext? Context,
    CoarseIdempotencyMetadata? Idempotency,
    LifecycleTransitionDefinition? LifecycleTransition,
    CommandSubmissionResponse? PriorOutcome,
    string? ReasonCode,
    string CorrelationId,
    string? TaskId)
{
    public bool IsAccepted => Kind == ChatBotCommandAdmissionDecisionKind.Accepted;

    public static ChatBotCommandAdmissionDecision Accepted(
        ChatBotGatewayContext context,
        CoarseIdempotencyMetadata idempotency,
        LifecycleTransitionDefinition lifecycleTransition)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(idempotency);
        ArgumentNullException.ThrowIfNull(lifecycleTransition);

        return new(
            ChatBotCommandAdmissionDecisionKind.Accepted,
            context,
            idempotency,
            lifecycleTransition,
            PriorOutcome: null,
            ReasonCode: null,
            context.Submission.CorrelationId,
            context.Submission.TaskId);
    }

    public static ChatBotCommandAdmissionDecision Rejected(string reasonCode, string correlationId, string? taskId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(reasonCode);
        ArgumentException.ThrowIfNullOrWhiteSpace(correlationId);

        return new(
            ChatBotCommandAdmissionDecisionKind.Rejected,
            Context: null,
            Idempotency: null,
            LifecycleTransition: null,
            PriorOutcome: null,
            reasonCode,
            correlationId,
            taskId);
    }

    public static ChatBotCommandAdmissionDecision ReplayPriorOutcome(CommandSubmissionResponse priorOutcome)
    {
        ArgumentNullException.ThrowIfNull(priorOutcome);

        return new(
            ChatBotCommandAdmissionDecisionKind.ReplayPriorOutcome,
            Context: null,
            Idempotency: null,
            LifecycleTransition: null,
            priorOutcome,
            ReasonCode: null,
            priorOutcome.CorrelationId,
            priorOutcome.TaskId);
    }
}

internal enum ChatBotCommandAdmissionDecisionKind
{
    Accepted,
    Rejected,
    ReplayPriorOutcome,
}
