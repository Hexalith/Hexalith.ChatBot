using Hexalith.ChatBot.Client.Generated;
using Hexalith.ChatBot.Server.Authentication;
using Hexalith.ChatBot.Server.Authorization;
using Hexalith.ChatBot.Contracts.Enums;
using Hexalith.ChatBot.Server.Audit;
using Hexalith.ChatBot.Server.Gateway.Idempotency;
using Hexalith.ChatBot.Server.Gateway.Status;
using Hexalith.ChatBot.Server.Gateway.Stages;
using Hexalith.ChatBot.Server.Lifecycle.StateModel;
using Hexalith.ChatBot.Server.Observability;
using Hexalith.EventStore.Contracts.Commands;

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
    ChatBotRequestAuthorizer requestAuthorizer,
    IChatBotMetrics? metrics = null,
    IAuthorizationFailureCounter? authorizationFailureCounter = null)
{
    /// <summary>Revalidates retained owner bounds immediately before SDK admission effects.</summary>
    public bool IsCurrent(ChatBotAuthorityPrincipal principal) => requestAuthorizer.IsCurrent(principal);

    /// <summary>Records the same safe authorization denial after admission as before admission.</summary>
    public ValueTask<ChatBotCommandAdmissionDecision> RejectAuthorityLapseAsync(ChatBotGatewayContext context, CancellationToken cancellationToken)
        => DenyAsync(context.Submission, context.TenantBinding.TenantId, context.Actor.ActorId, ChatBotAuthorizationReasonCodes.AuthorizationDenied, cancellationToken);

    private readonly IChatBotMetrics _metrics = metrics ?? NullChatBotMetrics.Instance;

    /// <summary>Audits a denied SDK transport binding without admitting caller-controlled envelope authority.</summary>
    public ValueTask<ChatBotCommandAdmissionDecision> RejectTransportAsync(CommandEnvelope command, ChatBotRequestContext? context, string reasonCode, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);
        ChatBotCommandSubmission metadata = new(context?.Principal ?? new System.Security.Claims.ClaimsPrincipal(),
            new CommandSubmissionRequest { CommandId = AuditMetadata.SafeOptionalToken(command.MessageId) ?? "unavailable", CommandType = AuditMetadata.SafeCommandName(command.CommandType) },
            AuditMetadata.SafeOptionalToken(command.CorrelationId) ?? "unavailable", AuditMetadata.SafeOptionalToken(command.Extensions?.GetValueOrDefault("taskId")), context?.Origin ?? ChatBotSurfaceOrigin.Api);
        return DenyAsync(metadata, context?.TenantId ?? "unavailable", context?.SubjectId ?? "anonymous", reasonCode, cancellationToken);
    }

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
        ChatBotRequestContext? trusted = actor.RequestContext;
        if (trusted is null && !ChatBotRequestContextResolver.TryResolve(actor.Principal, submission.Origin, out trusted, out _))
        {
            return await DenyAsync(submission, binding.TenantId, actor.ActorId, ChatBotAuthorizationReasonCodes.AuthorizationDenied, cancellationToken).ConfigureAwait(false);
        }

        if (trusted?.TenantId != binding.TenantId || trusted.SubjectId != actor.ActorId || trusted.Origin != submission.Origin)
        {
            return await DenyAsync(submission, binding.TenantId, actor.ActorId, ChatBotAuthorizationReasonCodes.AuthorizationDenied, cancellationToken).ConfigureAwait(false);
        }

        if (!requestAuthorizer.IsKnownOperation(submission.Request.CommandType ?? string.Empty, false))
        {
            await auditWriter.RecordAuthorizationFailureAsync(new ChatBotAuthorizationFailureAuditFact(binding.TenantId, actor.ActorId,
                AuditMetadata.SafeCommandName(submission.Request.CommandType), ChatBotAuthorizationReasonCodes.CommandNotAllowlisted,
                submission.CorrelationId, submission.TaskId, ChatBotSurfaceOrigins.ToWireValue(submission.Origin)), cancellationToken).ConfigureAwait(false);
            authorizationFailureCounter?.Record(binding.TenantId, clock.UtcNow);
            return ChatBotCommandAdmissionDecision.Rejected(ChatBotAuthorizationReasonCodes.CommandNotAllowlisted, submission.CorrelationId, submission.TaskId);
        }

        ChatBotAuthorityDecision authority = await requestAuthorizer.AuthorizeAsync(trusted, submission.Request.CommandType ?? string.Empty, false, submission.Request.Command, cancellationToken).ConfigureAwait(false);
        if (!authority.IsAllowed)
        {
            return await DenyAsync(submission, binding.TenantId, actor.ActorId, authority.ReasonCode, cancellationToken).ConfigureAwait(false);
        }

        actor = actor with { Principal = authority.Principal!, RequestContext = trusted };
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

        ChatBotGatewayContext context = new(submission, actor, binding, authorizationResult.ServiceClientGrantEvidence, authority.EvidenceReferences);
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

        if (!authority.Principal!.IsCurrent(clock.UtcNow))
        {
            return await DenyAsync(submission, binding.TenantId, actor.ActorId, ChatBotAuthorizationReasonCodes.AuthorizationDenied, cancellationToken).ConfigureAwait(false);
        }

        CoarseIdempotencyDecision idempotencyDecision = await idempotencyStore
            .RecordAdmissionAsync(context, cancellationToken)
            .ConfigureAwait(false);
        if (!authority.Principal!.IsCurrent(clock.UtcNow) && idempotencyDecision.Kind != CoarseIdempotencyDecisionKind.Proceed)
        {
            return await DenyAsync(submission, binding.TenantId, actor.ActorId, ChatBotAuthorizationReasonCodes.AuthorizationDenied, cancellationToken).ConfigureAwait(false);
        }

        try
        {
            if (idempotencyDecision.Kind == CoarseIdempotencyDecisionKind.RecoveryPending &&
                idempotencyStore is DaprCoarseIdempotencyStore daprStore)
            {
                idempotencyDecision = await RestoreQueuedOutcomeAsync(
                    daprStore, context, binding.TenantId, idempotencyDecision, cancellationToken).ConfigureAwait(false);
            }

            if (idempotencyDecision.Kind == CoarseIdempotencyDecisionKind.ReplayPriorOutcome)
            {
                RequireCurrentAuthority(context);
                await RecordDuplicateReplaySideEffectsAsync(context, idempotencyDecision, cancellationToken)
                    .ConfigureAwait(false);
                RequireCurrentAuthority(context);
                return ChatBotCommandAdmissionDecision.ReplayPriorOutcome(idempotencyDecision.PriorOutcome!);
            }
        }
        catch (ChatBotAuthorityLapsedException)
        {
            return await DenyAsync(submission, binding.TenantId, actor.ActorId, ChatBotAuthorizationReasonCodes.AuthorizationDenied, cancellationToken).ConfigureAwait(false);
        }

        if (idempotencyDecision.Kind == CoarseIdempotencyDecisionKind.Conflict)
        {
            return ChatBotCommandAdmissionDecision.Rejected(
                CoarseIdempotencyOperationClass.ConflictCodeFor(idempotencyDecision.Metadata.OperationClass),
                submission.CorrelationId,
                submission.TaskId);
        }

        if (idempotencyDecision.Kind == CoarseIdempotencyDecisionKind.RecoveryPending)
        {
            return ChatBotCommandAdmissionDecision.Rejected(
                "idempotency_outcome_unavailable",
                submission.CorrelationId,
                submission.TaskId);
        }

        string exceptionReason = "idempotency_outcome_unavailable";
        try
        {
            if (!authority.Principal!.IsCurrent(clock.UtcNow))
            {
                await AbortSafelyAsync(idempotencyDecision.Metadata, cancellationToken).ConfigureAwait(false);
                return await DenyAsync(submission, binding.TenantId, actor.ActorId, ChatBotAuthorizationReasonCodes.AuthorizationDenied, cancellationToken).ConfigureAwait(false);
            }

            LifecycleTransitionValidation lifecycleTransition = lifecycleTransitionGuard.ValidateCommandSubmission(context);
            if (!lifecycleTransition.IsValid)
            {
                AuditEnvelope rejectionEnvelope = AuditEnvelopeFactory.RejectedLifecycleTransition(context, lifecycleTransition, clock.UtcNow);
                exceptionReason = AuditFailureReasonCodes.AuditUnavailable;
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

                    await AbortSafelyAsync(idempotencyDecision.Metadata, cancellationToken).ConfigureAwait(false);

                    return ChatBotCommandAdmissionDecision.Rejected(
                        AuditFailureReasonCodes.AuditUnavailable,
                        submission.CorrelationId,
                        submission.TaskId);
                }

                await AbortSafelyAsync(idempotencyDecision.Metadata, cancellationToken).ConfigureAwait(false);

                return ChatBotCommandAdmissionDecision.Rejected(
                    LifecycleTransitionReasonCodes.InvalidTransition,
                    submission.CorrelationId,
                    submission.TaskId);
            }

            AuditEnvelope preCommitEnvelope = AuditEnvelopeFactory.PreCommit(context, lifecycleTransition.Transition, clock.UtcNow);
            exceptionReason = AuditFailureReasonCodes.AuditUnavailable;
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

                await AbortSafelyAsync(idempotencyDecision.Metadata, cancellationToken).ConfigureAwait(false);

                return ChatBotCommandAdmissionDecision.Rejected(
                    AuditFailureReasonCodes.AuditUnavailable,
                    submission.CorrelationId,
                    submission.TaskId);
            }

            if (!authority.Principal!.IsCurrent(clock.UtcNow))
            {
                await AbortSafelyAsync(idempotencyDecision.Metadata, cancellationToken).ConfigureAwait(false);
                return await DenyAsync(submission, binding.TenantId, actor.ActorId, ChatBotAuthorizationReasonCodes.AuthorizationDenied, cancellationToken).ConfigureAwait(false);
            }

            return ChatBotCommandAdmissionDecision.Accepted(context, idempotencyDecision.Metadata, lifecycleTransition.Transition);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            await AbortSafelyAsync(idempotencyDecision.Metadata, cancellationToken).ConfigureAwait(false);
            return ChatBotCommandAdmissionDecision.Rejected(exceptionReason, submission.CorrelationId, submission.TaskId);
        }
    }

    private async ValueTask AbortSafelyAsync(CoarseIdempotencyMetadata metadata, CancellationToken cancellationToken)
    {
        try
        {
            await idempotencyStore.AbortAdmissionAsync(metadata, cancellationToken).ConfigureAwait(false);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            // A failed durable cleanup retains its abort fence or bounded pre-dispatch lease.
        }
    }

    private async ValueTask RecordDuplicateReplaySideEffectsAsync(
        ChatBotGatewayContext context,
        CoarseIdempotencyDecision idempotencyDecision,
        CancellationToken cancellationToken)
    {
        RequireCurrentAuthority(context);
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

        RequireCurrentAuthority(context);
        OperationStatusRecord? existingStatus = await operationStatusStore
            .TryGetAsync(context.TenantBinding.TenantId, OperationStatusRecord.OperationIdFor(priorOutcome), cancellationToken)
            .ConfigureAwait(false);
        RequireCurrentAuthority(context);
        OperationStatusRecord replayStatus = existingStatus is not null
            ? existingStatus with { LastUpdatedAt = clock.UtcNow }
            : OperationStatusRecord.Accepted(context.TenantBinding.TenantId, priorOutcome, true, clock.UtcNow,
                idempotencyDecision.Metadata.OperationClass);
        replayStatus = replayStatus with { PriorOutcome = priorOutcome };
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

    private void RequireCurrentAuthority(ChatBotGatewayContext context)
    {
        if (context.Actor.Principal is ChatBotAuthorityPrincipal principal && !principal.IsCurrent(clock.UtcNow))
        {
            throw new ChatBotAuthorityLapsedException();
        }
    }

    private async ValueTask QueueReplayIntentAsync(
        AuditReplayIntentKind kind,
        AuditEnvelope envelope,
        string reasonCode,
        CancellationToken cancellationToken)
    {
        try
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
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            // Advisory notification cannot replace a safe admission failure.
        }
    }

    private async ValueTask AlertAsync(
        OperatorAlertKind kind,
        AuditEnvelope envelope,
        string reasonCode,
        CancellationToken cancellationToken)
    {
        try
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
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            // Advisory notification cannot replace a safe admission failure.
        }
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

    private async ValueTask<CoarseIdempotencyDecision> RestoreQueuedOutcomeAsync(
        DaprCoarseIdempotencyStore store,
        ChatBotGatewayContext context,
        string tenantId,
        CoarseIdempotencyDecision pending,
        CancellationToken cancellationToken)
    {
        IReadOnlyList<AuditReplayIntent> intents;
        try
        {
            intents = replayIntentQueue.Snapshot();
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            return pending;
        }

        foreach (AuditReplayIntent intent in intents)
        {
            if (intent.Kind != AuditReplayIntentKind.PostCommitAuditReconciliation ||
                intent.AcceptedOutcome is not { } outcome ||
                !string.Equals(intent.TenantId, tenantId, StringComparison.Ordinal) ||
                !string.Equals(intent.CoarseKeyHash, pending.Metadata.CoarseKeyHash, StringComparison.Ordinal) ||
                (pending.Metadata.OperationClass == CoarseIdempotencyOperationClass.CommandExecution.Code &&
                 !string.Equals(outcome.CommandId, context.Submission.Request.CommandId, StringComparison.Ordinal)))
            {
                continue;
            }

            try
            {
                RequireCurrentAuthority(context);
                bool reconciled = await store.ReconcileOutcomeAsync(intent, cancellationToken, pending.Metadata,
                    () => context.Actor.Principal is ChatBotAuthorityPrincipal principal && principal.IsCurrent(clock.UtcNow)).ConfigureAwait(false);
                RequireCurrentAuthority(context);
                if (!reconciled)
                {
                    continue;
                }
                await replayIntentQueue.AcknowledgeAsync(intent, cancellationToken).ConfigureAwait(false);
                RequireCurrentAuthority(context);
                return await idempotencyStore.RecordAdmissionAsync(context, cancellationToken).ConfigureAwait(false);
            }
            catch (Exception exception) when (exception is not OperationCanceledException and not ChatBotAuthorityLapsedException)
            {
                // One failed reconciliation cannot hide a later valid intent or replace the safe response.
            }
        }

        return pending;
    }
}
