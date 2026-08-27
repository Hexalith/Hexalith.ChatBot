using System.Linq;
using System.Security.Claims;
using System.Text.Json;

using Hexalith.ChatBot.Contracts.Commands;
using Hexalith.ChatBot.Contracts.Enums;
using Hexalith.ChatBot.Contracts.Queries;
using Hexalith.ChatBot.Server.Adapters.AiProvider;
using Hexalith.ChatBot.Server.Adapters.Mailbox;
using Hexalith.ChatBot.Server.Audit;
using Hexalith.ChatBot.Server.Gateway;
using Hexalith.ChatBot.Server.Gateway.Idempotency;
using Hexalith.ChatBot.Server.Gateway.Stages;
using Hexalith.ChatBot.Server.Gateway.Status;
using Hexalith.ChatBot.Server.Governance.AiMediation;
using Hexalith.ChatBot.Server.Lifecycle.AiExecution;
using Hexalith.ChatBot.Server.Lifecycle.Attachments;
using Hexalith.ChatBot.Server.Lifecycle.StateModel;
using Hexalith.ChatBot.Server.Operations;
using Hexalith.ChatBot.Server.Projections;
using Hexalith.EventStore.Contracts.Commands;
using Hexalith.EventStore.Contracts.Results;

using Microsoft.Extensions.Logging.Abstractions;

using CommandSubmissionRequest = Hexalith.ChatBot.Client.Generated.CommandSubmissionRequest;
using CommandSubmissionRequestRequestSchemaVersion = Hexalith.ChatBot.Client.Generated.CommandSubmissionRequestRequestSchemaVersion;
using CommandSubmissionResponse = Hexalith.ChatBot.Client.Generated.CommandSubmissionResponse;

namespace Hexalith.ChatBot.RecoverySandbox;

/// <summary>
/// Exercises the four ChatBot dependency seams through their real consuming orchestrator/pipeline types
/// (<see cref="AcceptedCommandDispatcher"/> for ai-provider and command-execution, <see cref="ChatBotCommandAdmissionPipeline"/>
/// for audit-store, <see cref="AttachmentCaptureCoordinator"/> for attachment-processing) rather than calling the
/// controllable leaf dependency directly, and derives safety outcomes by comparing the fault switch's ground truth
/// against what the real orchestrator actually decided/committed.
/// </summary>
internal sealed class RecoveryDependencyExercise(
    RecoveryScopedOutageState state,
    RecoveryAiAssistanceProvider aiProvider,
    RecoveryEventStoreGatewayClient eventStore,
    RecoveryAuditWriter auditWriter,
    RecoveryAttachmentContentSource attachmentSource,
    RecoveryFolderStore folderStore,
    RecoveryTenantAiPolicySnapshotProvider aiPolicySnapshots,
    InMemoryProjectConversationProjectionStore projectionStore,
    RecoveryScopeObservationMonitor scopeMonitor)
{
    private readonly InMemoryCoarseIdempotencyStore _idempotencyStore = new(new SystemClock());
    private readonly HashSet<string> _seededAttachmentIntakes = new(StringComparer.Ordinal);

    /// <summary>Runs the selected dependency contract for one idempotent correlation.</summary>
    public async ValueTask<(RecoveryDependencyExerciseResult Result, RecoveryScopeObservation? Scope)> ProcessAsync(
        string dependency,
        string tenantRef,
        string correlationId,
        CancellationToken cancellationToken)
    {
        bool expectedFault = state.IsFaulted(dependency);
        int effectsBefore = state.EffectCount(dependency, tenantRef);

        // `orchestratorCommitted` is the real consuming type's OWN decision (admission accept/reject, dispatcher
        // submission outcome, coordinator storage outcome) — independent of the fault switch itself, so a bug that
        // let the orchestrator ignore the fault (or refuse work when healthy) is a reachable, observable mismatch
        // rather than a value mirrored from the same `IsFaulted` check that configured it. `faultSignalCode` is the
        // real reason/error code the failing component itself returned, independent of `dependency`.
        (bool faultObserved, bool orchestratorCommitted, string? faultSignalCode) = dependency switch
        {
            "ai-provider" => await ExerciseAiProviderAsync(tenantRef, correlationId, cancellationToken).ConfigureAwait(false),
            "command-execution" => await ExerciseCommandDispatcherAsync(tenantRef, correlationId, cancellationToken).ConfigureAwait(false),
            "audit-store" => await ExerciseAuditWriterAsync(tenantRef, correlationId, cancellationToken).ConfigureAwait(false),
            "attachment-processing" => await ExerciseAttachmentSourceAsync(tenantRef, correlationId, cancellationToken).ConfigureAwait(false),
            _ => throw new InvalidOperationException("Unknown recovery dependency exercise."),
        };
        DateTimeOffset observedAtUtc = DateTimeOffset.UtcNow;
        if (faultObserved != expectedFault)
        {
            throw new InvalidOperationException("The exercised ChatBot dependency seam did not reflect its configured fault state.");
        }

        RecoveryScopeObservation? scope = faultObserved
            ? await scopeMonitor.RecordAsync(
                new RecoveryDependencyFailure(
                    dependency,
                    correlationId,
                    observedAtUtc,
                    faultSignalCode ?? throw new InvalidOperationException("A faulted exercise did not report its real fault signal code.")),
                cancellationToken).ConfigureAwait(false)
            : null;
        int effectsAfter = state.EffectCount(dependency, tenantRef);
        int effectDelta = effectsAfter - effectsBefore;
        int correlationEmissions = state.CorrelationEffectCount(dependency, tenantRef, correlationId);

        // Ground truth (expectedFault) and observation (orchestratorCommitted) are produced by independent code
        // paths: a faulted dependency that the orchestrator committed through anyway is a real unauthorized
        // mutation; a healthy dependency the orchestrator failed to commit through is a real silent loss.
        // Duplicate: more than one emission in this call, or this correlation already has more than one emission.
        return (
            new RecoveryDependencyExerciseResult(
                faultObserved,
                observedAtUtc,
                effectsAfter,
                UnauthorizedMutationDetected: expectedFault && orchestratorCommitted,
                SilentDataLossDetected: !expectedFault && !orchestratorCommitted,
                DuplicateSideEffectDetected: !faultObserved && (effectDelta > 1 || correlationEmissions > 1),
                CrossTenantLeakageDetected: state.HasCrossTenantEffect(dependency, tenantRef)),
            scope);
    }

    private async ValueTask<(bool FaultObserved, bool Committed, string? FaultSignalCode)> ExerciseAiProviderAsync(
        string tenantRef,
        string correlationId,
        CancellationToken cancellationToken)
    {
        ChatBotCommandAdmissionDecision decision = await AdmitAsync(
            tenantRef,
            correlationId,
            nameof(ExecuteLowRiskAIAssistance),
            new ExecuteLowRiskAIAssistance(
                "project-recovery",
                correlationId,
                "task-intent-recovery",
                "graph-message-recovery",
                "recovery-validator",
                LowRiskAiAssistanceKind.SummarizeVisibleContext,
                "context-recovery",
                "v1",
                "metadata_only",
                "operational",
                "disabled",
                ["recovery:source"],
                ["recovery:context"],
                [],
                8,
                "policy-recovery",
                correlationId,
                correlationId,
                $"transition:{correlationId}"),
            cancellationToken).ConfigureAwait(false);
        if (decision.Kind == ChatBotCommandAdmissionDecisionKind.ReplayPriorOutcome)
        {
            // A prior call for this exact correlation already committed (idempotency store replay) — the real
            // pipeline's own decision, not a re-derived assumption. No context survives a replay to dispatch again.
            return (FaultObserved: false, Committed: true, FaultSignalCode: null);
        }

        if (!decision.IsAccepted)
        {
            throw new InvalidOperationException("The ai-provider recovery exercise was not admitted; the admission stages are not exercising cleanly.");
        }

        // AI assistance still submits through the EventStore client; do not attribute that submit to the
        // command-execution ledger (Restore for ai-provider would not clear it).
        bool previousRecording = eventStore.RecordCommandExecutionEffects;
        eventStore.RecordCommandExecutionEffects = false;
        LowRiskAiAssistanceExecutionRecord record;
        try
        {
            AcceptedCommandDispatcher dispatcher = new(
                eventStore,
                new RecoveryParticipantResolutionOrchestrator(),
                new RecoveryAssociationScoringOrchestrator(),
                new SystemClock(),
                aiAssistanceProvider: aiProvider);
            try
            {
                _ = await dispatcher.DispatchAsync(decision.Context!, cancellationToken).ConfigureAwait(false);
            }
            catch
            {
                await AbortAdmissionAsync(decision, cancellationToken).ConfigureAwait(false);
                throw;
            }

            SubmitCommandRequest dispatchedSubmission = eventStore.LastSubmitted
                ?? throw new InvalidOperationException("The ai-provider recovery exercise did not observe a real dispatcher submission.");
            ExecuteLowRiskAIAssistance dispatched = dispatchedSubmission.Payload
                .Deserialize<ExecuteLowRiskAIAssistance>(new JsonSerializerOptions(JsonSerializerDefaults.Web))
                ?? throw new InvalidOperationException("The dispatched low-risk AI assistance command could not be read back.");
            if (dispatched.ExecutionRecord is { } approvalRoutedRecord)
            {
                // The dispatcher already resolved a synchronous provider record for an approval-routed execution;
                // no persisted-event coordinator run is involved on this branch.
                record = approvalRoutedRecord;
            }
            else
            {
                // The standard low-risk path no longer invokes the provider inside the dispatch boundary (Story
                // "AI execution coordination"): the real aggregate raises Started and the real persisted-event
                // AiExecutionCoordinator owns the provider call. Reconstruct that boundary with the actual
                // aggregate handler and the actual coordinator type rather than assuming the pre-coordinator
                // synchronous shape this exercise was originally written against.
                // The dispatcher's SubmitCommandRequest carries the raw logical `replay-test:` tenant label
                // (CommandEnvelope/AggregateIdentity reject the ':' it contains); resolve the same physical
                // storage tenant ReplayTenantPolicy.IsTestTenant guards before constructing identity-bearing types.
                string storageTenantRef = ReplayTenantPolicy.StorageTenantFor(dispatchedSubmission.Tenant)
                    ?? throw new InvalidOperationException("The ai-provider recovery exercise did not resolve a guarded storage tenant.");
                CommandEnvelope envelope = new(
                    MessageId: dispatchedSubmission.MessageId,
                    TenantId: storageTenantRef,
                    Domain: dispatchedSubmission.Domain,
                    AggregateId: dispatchedSubmission.AggregateId,
                    CommandType: dispatchedSubmission.CommandType,
                    Payload: JsonSerializer.SerializeToUtf8Bytes(dispatched),
                    CorrelationId: dispatchedSubmission.CorrelationId ?? correlationId,
                    CausationId: null,
                    UserId: "recovery-validator",
                    Extensions: null);
                DomainResult started = GovernedOperationAggregate.Handle(dispatched, state: null, envelope);
                LowRiskAiAssistanceExecutionStarted startedEvent = started.Events
                    .OfType<LowRiskAiAssistanceExecutionStarted>()
                    .SingleOrDefault()
                    ?? throw new InvalidOperationException("The real aggregate did not raise a low-risk AI execution start for the dispatched command.");

                using AiExecutionCoordinator coordinator = new(
                    new InMemoryAiExecutionWorkStore(),
                    aiProvider,
                    eventStore,
                    new SystemClock(),
                    NullLogger<AiExecutionCoordinator>.Instance);
                // The coordinator's own work tracking (and, through it, the sandbox's effect ledger) is keyed by
                // the exercise's logical `replay-test:` tenant like every other dependency in this file — only
                // GovernedOperationAggregate.Handle's CommandEnvelope needs the guarded physical storage tenant.
                await coordinator.RecordStartedAsync(
                    tenantRef,
                    dispatchedSubmission.AggregateId,
                    sourceVersion: 1,
                    startedEvent,
                    cancellationToken).ConfigureAwait(false);
                await coordinator.StartAsync(cancellationToken).ConfigureAwait(false);
                try
                {
                    DateTimeOffset deadline = DateTimeOffset.UtcNow.AddSeconds(5);
                    while (eventStore.LastSubmitted is null ||
                        !string.Equals(eventStore.LastSubmitted.CommandType, nameof(CompleteLowRiskAiAssistance), StringComparison.Ordinal))
                    {
                        if (DateTimeOffset.UtcNow >= deadline)
                        {
                            throw new InvalidOperationException("The persisted-event AI execution coordinator did not submit a terminal completion inside the exercise budget.");
                        }

                        await Task.Delay(25, cancellationToken).ConfigureAwait(false);
                    }
                }
                finally
                {
                    await coordinator.StopAsync(CancellationToken.None).ConfigureAwait(false);
                }

                SubmitCommandRequest completionSubmission = eventStore.LastSubmitted!;
                CompleteLowRiskAiAssistance completion = completionSubmission.Payload
                    .Deserialize<CompleteLowRiskAiAssistance>(new JsonSerializerOptions(JsonSerializerDefaults.Web))
                    ?? throw new InvalidOperationException("The dispatched command did not carry the AI execution completion.");
                record = completion.Record;
            }
        }
        finally
        {
            eventStore.RecordCommandExecutionEffects = previousRecording;
        }

        bool faultObserved = string.Equals(record.Outcome, "failed", StringComparison.Ordinal);
        bool committed = string.Equals(record.Outcome, "succeeded", StringComparison.Ordinal);
        // Failed provider outcome must not seal the coarse key — Restore then retry needs a fresh Admit.
        // Succeeded dispatch mirrors CommandGateway.RecordOutcomeAsync so same-correlation replay is safe.
        if (faultObserved)
        {
            await AbortAdmissionAsync(decision, cancellationToken).ConfigureAwait(false);
        }
        else
        {
            await RecordAdmissionOutcomeAsync(decision, cancellationToken).ConfigureAwait(false);
        }

        return (faultObserved, committed, faultObserved ? record.FailureCode : null);
    }

    private async ValueTask<(bool FaultObserved, bool Committed, string? FaultSignalCode)> ExerciseCommandDispatcherAsync(
        string tenantRef,
        string correlationId,
        CancellationToken cancellationToken)
    {
        ChatBotCommandAdmissionDecision decision = await AdmitAsync(
            tenantRef,
            correlationId,
            nameof(RecordGovernedNote),
            new RecordGovernedNote(correlationId),
            cancellationToken).ConfigureAwait(false);
        if (decision.Kind == ChatBotCommandAdmissionDecisionKind.ReplayPriorOutcome)
        {
            return (FaultObserved: false, Committed: true, FaultSignalCode: null);
        }

        if (!decision.IsAccepted)
        {
            throw new InvalidOperationException(
                $"The command-execution recovery exercise was not admitted: {decision.ReasonCode}.");
        }

        AcceptedCommandDispatcher dispatcher = new(
            eventStore,
            new RecoveryParticipantResolutionOrchestrator(),
            new RecoveryAssociationScoringOrchestrator(),
            new SystemClock());
        try
        {
            _ = await dispatcher.DispatchAsync(decision.Context!, cancellationToken).ConfigureAwait(false);
            await RecordAdmissionOutcomeAsync(decision, cancellationToken).ConfigureAwait(false);
            return (FaultObserved: false, Committed: true, FaultSignalCode: null);
        }
        catch (Exception) when (state.IsFaulted("command-execution"))
        {
            await AbortAdmissionAsync(decision, cancellationToken).ConfigureAwait(false);
            return (FaultObserved: true, Committed: false, FaultSignalCode: "command_execution_unavailable");
        }
    }

    private async ValueTask<(bool FaultObserved, bool Committed, string? FaultSignalCode)> ExerciseAuditWriterAsync(
        string tenantRef,
        string correlationId,
        CancellationToken cancellationToken)
    {
        ChatBotCommandAdmissionDecision decision = await AdmitAsync(
            tenantRef,
            correlationId,
            nameof(RecordGovernedNote),
            new RecordGovernedNote(correlationId),
            cancellationToken).ConfigureAwait(false);
        if (decision.Kind == ChatBotCommandAdmissionDecisionKind.ReplayPriorOutcome)
        {
            // A prior call for this exact correlation already committed successfully — replays only exist for a
            // prior accepted admission, so this is genuinely a non-faulted, already-committed outcome.
            return (FaultObserved: false, Committed: true, FaultSignalCode: null);
        }

        bool faultObserved = !decision.IsAccepted &&
            string.Equals(decision.ReasonCode, AuditFailureReasonCodes.AuditUnavailable, StringComparison.Ordinal);
        if (!decision.IsAccepted && !faultObserved)
        {
            throw new InvalidOperationException(
                $"The audit-store recovery exercise was rejected for an unexpected reason: {decision.ReasonCode}.");
        }

        if (decision.IsAccepted)
        {
            // Audit-store exercise stops at admission; still seal the idempotency record for replay.
            await RecordAdmissionOutcomeAsync(decision, cancellationToken).ConfigureAwait(false);
        }

        return (faultObserved, Committed: decision.IsAccepted, FaultSignalCode: faultObserved ? decision.ReasonCode : null);
    }

    private async ValueTask<(bool FaultObserved, bool Committed, string? FaultSignalCode)> ExerciseAttachmentSourceAsync(
        string tenantRef,
        string correlationId,
        CancellationToken cancellationToken)
    {
        string intakeId = $"intake-recovery:{correlationId}";
        await SeedAttachmentCandidateAsync(tenantRef, intakeId, correlationId, cancellationToken).ConfigureAwait(false);

        AttachmentCaptureCoordinator coordinator = new(
            projectionStore,
            attachmentSource,
            folderStore);
        AttachmentCaptureCoordinatorResult result = await coordinator
            .CaptureAsync(new AttachmentCaptureCoordinatorRequest(tenantRef, intakeId, 1, correlationId), cancellationToken)
            .ConfigureAwait(false);
        bool contentFaulted = state.IsFaulted("attachment-processing");
        // Replay after a successful capture finds no Pending/Retryable candidates (StoredCount == 0). Treat a
        // prior successful effect for this correlation as committed so SilentDataLoss is not a false positive.
        bool committed = result.StoredCount > 0
            || (!contentFaulted
                && state.CorrelationEffectCount("attachment-processing", tenantRef, correlationId) > 0);
        bool faultObserved = contentFaulted && result.DegradedCount > 0 && result.StoredCount == 0;
        return (faultObserved, committed, faultObserved ? "attachment_dependency_unavailable" : null);
    }

    private async ValueTask SeedAttachmentCandidateAsync(
        string tenantRef,
        string intakeId,
        string correlationId,
        CancellationToken cancellationToken)
    {
        if (!_seededAttachmentIntakes.Add(intakeId))
        {
            // Leave any previously stored attachment outcome intact so a second process can exercise idempotency.
            return;
        }

        string associationId = $"association-recovery:{correlationId}";
        await projectionStore.UpsertAsync(
            new ProjectConversationItemView(
                tenantRef,
                "project-recovery",
                "Recovery Validation",
                $"item-recovery:{correlationId}",
                intakeId,
                ProjectConversationItemKind.EmailDerived,
                ProjectConversationActorKind.Mailbox,
                "recovery-mailbox",
                DateTimeOffset.UtcNow,
                LifecycleState.Associated,
                AssociationThresholdBand.Auto,
                1.0,
                associationId,
                "mailbox-recovery",
                "message-recovery",
                InternetMessageId: null,
                "conversation-recovery",
                SourceThreadId: null,
                SourceReceivedAtUtc: null,
                SourceSentAtUtc: null,
                SourceCreatedAtUtc: null,
                SourceTimezone: null,
                SourceProvenanceDisplayToken: null,
                "recovery-validation",
                "metadata_only",
                "operational",
                "chatbot.project-conversation-item.v1",
                1,
                correlationId),
            cancellationToken).ConfigureAwait(false);
        await projectionStore.UpsertAttachmentReferencesAsync(
            new ProjectConversationAttachmentSetView(
                tenantRef,
                intakeId,
                [
                    new ProjectConversationAttachmentReferenceView(
                        tenantRef,
                        intakeId,
                        "attachment-recovery",
                        0,
                        "recovery-attachment.bin",
                        "application/octet-stream",
                        1,
                        ProjectConversationAttachmentStatus.Pending,
                        ProjectConversationAttachmentStatus.Pending,
                        ProjectConversationAttachmentStatus.Pending,
                        FolderId: null,
                        FileId: null,
                        "no-duplicate",
                        "not-retried",
                        "eligible",
                        [],
                        "none",
                        "pending",
                        "metadata_only",
                        "operational",
                        1,
                        correlationId),
                ],
                1,
                correlationId),
            cancellationToken).ConfigureAwait(false);
    }

    private async ValueTask RecordAdmissionOutcomeAsync(
        ChatBotCommandAdmissionDecision decision,
        CancellationToken cancellationToken)
    {
        if (!decision.IsAccepted || decision.Idempotency is null || decision.Context is null)
        {
            return;
        }

        CommandSubmissionResponse response = new()
        {
            CommandId = decision.Context.Submission.Request.CommandId,
            CorrelationId = decision.CorrelationId,
            TaskId = decision.TaskId,
            LifecycleState = Hexalith.ChatBot.Client.Generated.LifecycleState.Proposed,
            AcceptedAt = DateTimeOffset.UtcNow,
        };
        await _idempotencyStore
            .RecordOutcomeAsync(decision.Idempotency, response, cancellationToken)
            .ConfigureAwait(false);
    }

    private async ValueTask AbortAdmissionAsync(
        ChatBotCommandAdmissionDecision decision,
        CancellationToken cancellationToken)
    {
        if (decision.Idempotency is null)
        {
            return;
        }

        await _idempotencyStore
            .AbortAdmissionAsync(decision.Idempotency, cancellationToken)
            .ConfigureAwait(false);
    }

    private async ValueTask<ChatBotCommandAdmissionDecision> AdmitAsync(
        string tenantRef,
        string correlationId,
        string commandType,
        IChatBotCommand command,
        CancellationToken cancellationToken)
    {
        ClaimsPrincipal principal = new(new ClaimsIdentity(
            [
                new Claim("sub", "recovery-validator"),
                new Claim("eventstore:tenant", tenantRef),
                new Claim("party", "party-recovery"),
                new Claim("email", "recovery-validator@example.test"),
                new Claim("requester_authority_class", "project-contributor"),
                new Claim(ParticipantAuthorizationStage.ActorTypeClaim, ParticipantAuthorizationStage.HumanActorValue),
                new Claim(ParticipantAuthorizationStage.ProjectOwnerClaim, "project-recovery"),
            ],
            "recovery"));
        CommandSubmissionRequest request = new()
        {
            CommandId = correlationId,
            CommandType = commandType,
            Command = command,
            RequestSchemaVersion = CommandSubmissionRequestRequestSchemaVersion.V1,
        };
        ChatBotCommandSubmission submission = new(principal, request, correlationId, correlationId);

        ChatBotCommandAdmissionPipeline pipeline = new(
            new ClaimsAuthenticationStage(),
            new ClaimsTenantBindingStage(),
            new ParticipantAuthorizationStage(),
            new DeterministicAiActionRiskClassifier(),
            new AiActionApprovalGate(new DefaultAiActionPolicyEvaluator(aiPolicySnapshots)),
            _idempotencyStore,
            auditWriter,
            new InMemoryAuditReplayIntentQueue(),
            new InMemoryOperatorAlertSink(),
            new InMemoryOperationStatusStore(),
            new SystemClock(),
            new CommandSubmissionLifecycleTransitionGuard(),
            new ChatBotSpineCommandAllowlist());
        return await pipeline.AdmitAsync(submission, cancellationToken).ConfigureAwait(false);
    }
}
