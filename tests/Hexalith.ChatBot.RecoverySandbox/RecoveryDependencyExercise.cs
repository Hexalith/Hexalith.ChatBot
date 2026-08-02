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
using Hexalith.ChatBot.Server.Lifecycle.Attachments;
using Hexalith.ChatBot.Server.Lifecycle.StateModel;
using Hexalith.ChatBot.Server.Projections;
using Hexalith.EventStore.Contracts.Commands;

using CommandSubmissionRequest = Hexalith.ChatBot.Client.Generated.CommandSubmissionRequest;
using CommandSubmissionRequestRequestSchemaVersion = Hexalith.ChatBot.Client.Generated.CommandSubmissionRequestRequestSchemaVersion;

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

        // Ground truth (expectedFault) and observation (orchestratorCommitted) are produced by independent code
        // paths: a faulted dependency that the orchestrator committed through anyway is a real unauthorized
        // mutation; a healthy dependency the orchestrator failed to commit through is a real silent loss.
        return (
            new RecoveryDependencyExerciseResult(
                faultObserved,
                observedAtUtc,
                effectsAfter,
                UnauthorizedMutationDetected: expectedFault && orchestratorCommitted,
                SilentDataLossDetected: !expectedFault && !orchestratorCommitted,
                DuplicateSideEffectDetected: effectsAfter - effectsBefore > 1,
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

        AcceptedCommandDispatcher dispatcher = new(
            eventStore,
            new RecoveryParticipantResolutionOrchestrator(),
            new RecoveryAssociationScoringOrchestrator(),
            new SystemClock(),
            aiAssistanceProvider: aiProvider);
        _ = await dispatcher.DispatchAsync(decision.Context!, cancellationToken).ConfigureAwait(false);

        SubmitCommandRequest submitted = eventStore.LastSubmitted
            ?? throw new InvalidOperationException("The ai-provider recovery exercise did not observe a real dispatcher submission.");
        LowRiskAiAssistanceExecutionRecord record = submitted.Payload
            .GetProperty("ExecutionRecord")
            .Deserialize<LowRiskAiAssistanceExecutionRecord>(new JsonSerializerOptions(JsonSerializerDefaults.Web))
            ?? throw new InvalidOperationException("The dispatched command did not carry the AI execution record.");
        bool faultObserved = string.Equals(record.Outcome, "failed", StringComparison.Ordinal);
        bool committed = string.Equals(record.Outcome, "succeeded", StringComparison.Ordinal);
        return (faultObserved, committed, faultObserved ? record.FailureCode : null);
    }

    private async ValueTask<(bool FaultObserved, bool Committed, string? FaultSignalCode)> ExerciseCommandDispatcherAsync(
        string tenantRef,
        string correlationId,
        CancellationToken cancellationToken)
    {
        ClaimsPrincipal principal = new(new ClaimsIdentity([new Claim(ClaimTypes.NameIdentifier, "recovery-validator")], "recovery"));
        CommandSubmissionRequest request = new()
        {
            CommandId = correlationId,
            CommandType = nameof(RecordGovernedNote),
            Command = new RecordGovernedNote(correlationId),
            RequestSchemaVersion = CommandSubmissionRequestRequestSchemaVersion.V1,
        };
        ChatBotCommandSubmission submission = new(principal, request, correlationId, correlationId);
        ChatBotGatewayContext context = new(
            submission,
            new ChatBotAuthenticatedActor("recovery-validator", principal),
            new ChatBotTenantBinding(tenantRef));
        AcceptedCommandDispatcher dispatcher = new(
            eventStore,
            new RecoveryParticipantResolutionOrchestrator(),
            new RecoveryAssociationScoringOrchestrator(),
            new SystemClock());
        try
        {
            _ = await dispatcher.DispatchAsync(context, cancellationToken).ConfigureAwait(false);
            return (FaultObserved: false, Committed: true, FaultSignalCode: null);
        }
        catch (HttpRequestException) when (state.IsFaulted("command-execution"))
        {
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
        bool committed = result.StoredCount > 0;
        bool faultObserved = result.DegradedCount > 0 && result.StoredCount == 0;
        return (faultObserved, committed, faultObserved ? "attachment_dependency_unavailable" : null);
    }

    private async ValueTask SeedAttachmentCandidateAsync(
        string tenantRef,
        string intakeId,
        string correlationId,
        CancellationToken cancellationToken)
    {
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
            new InMemoryCoarseIdempotencyStore(new SystemClock()),
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
