using System.Text.Json;

using Hexalith.ChatBot.Contracts.Commands;
using Hexalith.ChatBot.Contracts.Enums;
using Hexalith.ChatBot.Contracts.Identities;
using Hexalith.ChatBot.Contracts.Queries;
using Hexalith.ChatBot.Server.Audit;
using Hexalith.ChatBot.Server.Adapters.AiProvider;
using Hexalith.ChatBot.Server.Adapters.Conversations;
using Hexalith.ChatBot.Server.Gateway;
using Hexalith.ChatBot.Server.Governance.AiMediation;
using Hexalith.ChatBot.Server.Lifecycle.Workflows;
using Hexalith.ChatBot.Server.Operations;
using Hexalith.EventStore.Client.Gateway;
using Hexalith.EventStore.Contracts.Commands;

namespace Hexalith.ChatBot.Server.Gateway.Stages;

/// <summary>
/// Real EventStore dispatch behind the <see cref="ICommandDispatcher"/> seam. It routes an admitted command
/// into EventStore through the public gateway client — the durable segment of the spine
/// (<c>fine-idempotency → execute → persist → publish → project</c>) runs inside EventStore — and forwards
/// correlation + task provenance. <see cref="CommandGateway"/> remains the single caller of <see cref="DispatchAsync"/>.
/// </summary>
internal sealed class AcceptedCommandDispatcher(
    IEventStoreGatewayClient eventStore,
    IParticipantResolutionOrchestrator participantResolution,
    IAssociationScoringOrchestrator associationScoring,
    ISystemClock clock,
    IAiAssistanceProvider? aiAssistanceProvider = null,
    ICorrectionPropagationCoordinator? correctionPropagation = null,
    IApprovedAiActionCommandAllowlist? approvedAiActionAllowlist = null,
    IConversationWriter? conversationWriter = null) : ICommandDispatcher
{
    // The EventStoreAggregate base deserializes the command payload with default (case-sensitive, PascalCase)
    // JsonSerializer options. The inbound wire body is camelCase, so we read it case-insensitively (web options)
    // and re-serialize PascalCase (default options) — otherwise the engine would fail to bind the payload.
    private static readonly JsonSerializerOptions ReadOptions = new(JsonSerializerDefaults.Web);

    public async ValueTask<ChatBotDispatchResult> DispatchAsync(ChatBotGatewayContext context, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);

        EventStoreDispatchPlan plan = await BuildPlanAsync(context, cancellationToken).ConfigureAwait(false);
        SubmitCommandRequest request = new(
            MessageId: context.Submission.Request.CommandId,
            Tenant: context.TenantBinding.TenantId,
            Domain: ChatBotEventStore.DomainName,
            AggregateId: plan.AggregateId,
            CommandType: plan.CommandType,
            Payload: plan.Payload,
            CorrelationId: context.Submission.CorrelationId,
            Extensions: BuildExtensions(context));

        _ = await eventStore.SubmitCommandAsync(request, cancellationToken).ConfigureAwait(false);

        if (plan.CorrectionPropagation is not null && correctionPropagation is not null)
        {
            await correctionPropagation
                .StartAsync(plan.CorrectionPropagation, cancellationToken)
                .ConfigureAwait(false);
        }

        return new ChatBotDispatchResult(clock.UtcNow, plan.AggregateId);
    }

    private async ValueTask<EventStoreDispatchPlan> BuildPlanAsync(ChatBotGatewayContext context, CancellationToken cancellationToken)
    {
        string commandType = context.Submission.Request.CommandType ?? string.Empty;
        JsonElement command = ToElement(context.Submission.Request.Command);

        if (string.Equals(commandType, nameof(RecordGovernedNote), StringComparison.Ordinal))
        {
            RecordGovernedNote note = command.Deserialize<RecordGovernedNote>(ReadOptions)
                ?? throw new InvalidOperationException("The governed note command payload could not be read.");
            if (string.IsNullOrWhiteSpace(note.NoteId))
            {
                throw new InvalidOperationException("The governed note command is missing its aggregate identity.");
            }

            // PascalCase payload (default options) so the case-sensitive aggregate engine round-trips it.
            JsonElement payload = JsonSerializer.SerializeToElement(note);
            return new EventStoreDispatchPlan(note.NoteId, commandType, payload);
        }

        if (string.Equals(commandType, nameof(CaptureMailboxMessageIntake), StringComparison.Ordinal))
        {
            CaptureMailboxMessageIntake intake = command.Deserialize<CaptureMailboxMessageIntake>(ReadOptions)
                ?? throw new InvalidOperationException("The mailbox-intake command payload could not be read.");
            if (!MailboxMessageIntakeId.TryParse(intake.IntakeId, out _))
            {
                throw new InvalidOperationException("The mailbox-intake command is missing its aggregate identity.");
            }

            if (string.IsNullOrWhiteSpace(intake.Source.ProviderMessageId) ||
                string.IsNullOrWhiteSpace(intake.Source.MailboxId))
            {
                throw new InvalidOperationException("The mailbox-intake command is missing its source identity.");
            }

            JsonElement payload = JsonSerializer.SerializeToElement(intake);
            return new EventStoreDispatchPlan(intake.IntakeId, commandType, payload);
        }

        if (string.Equals(commandType, nameof(ResolveMailboxMessageParticipants), StringComparison.Ordinal))
        {
            ResolveMailboxMessageParticipants commandPayload = command.Deserialize<ResolveMailboxMessageParticipants>(ReadOptions)
                ?? throw new InvalidOperationException("The participant-resolution command payload could not be read.");
            if (!ParticipantResolutionId.TryParse(commandPayload.ResolutionId, out _) ||
                !MailboxMessageIntakeId.TryParse(commandPayload.IntakeId, out _))
            {
                throw new InvalidOperationException("The participant-resolution command is missing its aggregate identity.");
            }

            if (commandPayload.SourceParticipants is null ||
                string.IsNullOrWhiteSpace(commandPayload.SourceMailboxId) ||
                string.IsNullOrWhiteSpace(commandPayload.ResolutionKernelVersion))
            {
                throw new InvalidOperationException("The participant-resolution command is missing its source identity.");
            }

            ResolveMailboxMessageParticipants resolved = await participantResolution
                .ResolveAsync(commandPayload, context, cancellationToken)
                .ConfigureAwait(false);
            JsonElement payload = JsonSerializer.SerializeToElement(resolved);
            return new EventStoreDispatchPlan(resolved.ResolutionId, commandType, payload);
        }

        if (string.Equals(commandType, nameof(ScoreMailboxMessageAssociation), StringComparison.Ordinal))
        {
            ScoreMailboxMessageAssociation commandPayload = command.Deserialize<ScoreMailboxMessageAssociation>(ReadOptions)
                ?? throw new InvalidOperationException("The association-scoring command payload could not be read.");
            if (!AssociationWorkflowId.TryParse(commandPayload.AssociationId, out _) ||
                !MailboxMessageIntakeId.TryParse(commandPayload.IntakeId, out _))
            {
                throw new InvalidOperationException("The association-scoring command is missing its aggregate identity.");
            }

            if (commandPayload.DeterministicSignals is null ||
                commandPayload.DeterministicSignals.Count == 0 ||
                string.IsNullOrWhiteSpace(commandPayload.SourceMailboxId) ||
                string.IsNullOrWhiteSpace(commandPayload.SourceConversationId))
            {
                throw new InvalidOperationException("The association-scoring command is missing its deterministic evidence.");
            }

            ScoreMailboxMessageAssociation scored = await associationScoring
                .ScoreAsync(commandPayload, context, cancellationToken)
                .ConfigureAwait(false);
            JsonElement payload = JsonSerializer.SerializeToElement(scored);
            return new EventStoreDispatchPlan(scored.AssociationId, commandType, payload);
        }

        if (string.Equals(commandType, nameof(SetAssociationConfidenceThresholds), StringComparison.Ordinal))
        {
            SetAssociationConfidenceThresholds commandPayload = command.Deserialize<SetAssociationConfidenceThresholds>(ReadOptions)
                ?? throw new InvalidOperationException("The association-threshold command payload could not be read.");
            if (string.IsNullOrWhiteSpace(commandPayload.PolicyId) ||
                string.IsNullOrWhiteSpace(commandPayload.PolicyVersion))
            {
                throw new InvalidOperationException("The association-threshold command is missing its aggregate identity.");
            }

            JsonElement payload = JsonSerializer.SerializeToElement(commandPayload with { ChangedAt = clock.UtcNow });
            return new EventStoreDispatchPlan(commandPayload.PolicyId, commandType, payload);
        }

        if (string.Equals(commandType, nameof(RequestFailedWorkflowRetry), StringComparison.Ordinal))
        {
            RequestFailedWorkflowRetry retry = command.Deserialize<RequestFailedWorkflowRetry>(ReadOptions)
                ?? throw new InvalidOperationException("The workflow-retry command payload could not be read.");
            if (string.IsNullOrWhiteSpace(retry.RetryId) ||
                string.IsNullOrWhiteSpace(retry.FailedEventId) ||
                string.IsNullOrWhiteSpace(retry.FailedOperationClass) ||
                string.IsNullOrWhiteSpace(retry.FailureReasonCode) ||
                retry.ExpectedFailedSourceVersion <= 0)
            {
                throw new InvalidOperationException("The workflow-retry command is missing its retry metadata.");
            }

            JsonElement payload = JsonSerializer.SerializeToElement(retry);
            return new EventStoreDispatchPlan(retry.RetryId, commandType, payload);
        }

        if (string.Equals(commandType, nameof(ProposeAIAction), StringComparison.Ordinal))
        {
            ProposeAIAction proposal = command.Deserialize<ProposeAIAction>(ReadOptions)
                ?? throw new InvalidOperationException("The AI action proposal command payload could not be read.");
            if (string.IsNullOrWhiteSpace(proposal.TaskIntentId) ||
                string.IsNullOrWhiteSpace(proposal.SourceMessageId) ||
                context.RiskClassification?.Record is null)
            {
                throw new InvalidOperationException("The AI action proposal command is missing its classification metadata.");
            }

            JsonElement payload = JsonSerializer.SerializeToElement(proposal with { RiskClassification = context.RiskClassification.Record });
            return new EventStoreDispatchPlan(proposal.SourceMessageId, commandType, payload);
        }

        if (string.Equals(commandType, nameof(ExecuteLowRiskAIAssistance), StringComparison.Ordinal))
        {
            ExecuteLowRiskAIAssistance execution = command.Deserialize<ExecuteLowRiskAIAssistance>(ReadOptions)
                ?? throw new InvalidOperationException("The low-risk AI assistance execution command payload could not be read.");
            if (string.IsNullOrWhiteSpace(execution.ExecutionId) ||
                string.IsNullOrWhiteSpace(execution.ProposalId) ||
                string.IsNullOrWhiteSpace(execution.ContextPackageId) ||
                context.RiskClassification?.Record is null ||
                context.ApprovalResult?.Kind is not (ChatBotApprovalResultKind.AllowedLowRiskExecution or ChatBotApprovalResultKind.RoutedToApproval))
            {
                throw new InvalidOperationException("The low-risk AI assistance execution command is missing trusted admission metadata.");
            }

            string policySnapshotId = context.ApprovalResult.PolicySnapshotId ?? execution.PolicySnapshotId ?? "unavailable";
            string assistanceKind = AiActionApprovalGate.AssistanceKindToken(execution.AssistanceKind);
            LowRiskAiAssistanceExecutionRecord providerRecord = context.ApprovalResult.Kind is ChatBotApprovalResultKind.RoutedToApproval
                ? RoutedToApprovalRecord(context, execution, policySnapshotId, assistanceKind)
                : await InvokeProviderAsync(context, execution, policySnapshotId, assistanceKind, cancellationToken).ConfigureAwait(false);

            ExecuteLowRiskAIAssistance enriched = execution with
            {
                PolicySnapshotId = policySnapshotId,
                RiskClassification = context.RiskClassification.Record,
                ExecutionRecord = providerRecord,
            };
            JsonElement payload = JsonSerializer.SerializeToElement(enriched);
            return new EventStoreDispatchPlan(enriched.SourceMessageId, commandType, payload);
        }

        if (string.Equals(commandType, nameof(DecideAiActionApproval), StringComparison.Ordinal))
        {
            DecideAiActionApproval decision = command.Deserialize<DecideAiActionApproval>(ReadOptions)
                ?? throw new InvalidOperationException("The AI action approval decision command payload could not be read.");
            if (string.IsNullOrWhiteSpace(decision.ProjectId) ||
                string.IsNullOrWhiteSpace(decision.ApprovalId) ||
                string.IsNullOrWhiteSpace(decision.ProposalId) ||
                string.IsNullOrWhiteSpace(decision.SourceMessageId) ||
                string.IsNullOrWhiteSpace(decision.DecisionId) ||
                decision.ExpectedApprovalSourceVersion <= 0 ||
                string.IsNullOrWhiteSpace(decision.CorrelationId) ||
                string.IsNullOrWhiteSpace(decision.RationaleRedactionState) ||
                string.IsNullOrWhiteSpace(decision.SchemaVersion))
            {
                throw new InvalidOperationException("The AI action approval decision command is missing trusted decision metadata.");
            }

            JsonElement payload = JsonSerializer.SerializeToElement(decision);
            return new EventStoreDispatchPlan(decision.SourceMessageId, commandType, payload);
        }

        if (string.Equals(commandType, nameof(ExecuteApprovedAIAction), StringComparison.Ordinal))
        {
            ExecuteApprovedAIAction execution = command.Deserialize<ExecuteApprovedAIAction>(ReadOptions)
                ?? throw new InvalidOperationException("The approved AI action execution command payload could not be read.");
            IApprovedAiActionCommandAllowlist allowlist = approvedAiActionAllowlist
                ?? new ApprovedAiActionCommandAllowlist();
            if (string.IsNullOrWhiteSpace(execution.ExecutionId) ||
                string.IsNullOrWhiteSpace(execution.ProposalId) ||
                string.IsNullOrWhiteSpace(execution.ApprovalId) ||
                string.IsNullOrWhiteSpace(execution.SourceMessageId) ||
                string.IsNullOrWhiteSpace(execution.CommandName) ||
                !allowlist.IsAllowed(execution.CommandName, execution.CommandAllowlistVersion))
            {
                throw new InvalidOperationException("The approved AI action execution command is missing trusted allowlist metadata.");
            }

            IConversationWriter writer = conversationWriter
                ?? throw new InvalidOperationException("The conversation writer is not configured.");
            string policySnapshotId = execution.PolicySnapshotId ?? "unavailable";
            string auditOperationId = $"audit:{execution.ExecutionId}";
            ConversationAppendResult append = await writer
                .PrepareAppendConversationMessageAsync(
                    new ApprovedAiConversationAppendRequest(
                        context.TenantBinding.TenantId,
                        execution.ProjectId,
                        execution.RequesterId,
                        execution.ProposalId,
                        execution.ApprovalId,
                        execution.ExecutionId,
                        execution.SourceMessageId,
                        execution.SourceConversationItemId,
                        execution.CommandName,
                        execution.CommandAllowlistVersion,
                        policySnapshotId,
                        context.Submission.CorrelationId,
                        auditOperationId),
                    cancellationToken)
                .ConfigureAwait(false);

            ApprovedAiActionExecutionRecord record = new(
                execution.ExecutionId,
                execution.ProposalId,
                execution.ApprovalId,
                execution.CommandName,
                execution.CommandAllowlistVersion,
                append.Outcome,
                clock.UtcNow,
                auditOperationId,
                append.AuditStatus,
                context.Submission.CorrelationId,
                append.GeneratedContentVisibility,
                append.SafeNextAction,
                append.FailureCode,
                append.Retryability,
                execution.RedactionState,
                execution.RetentionClass);

            JsonElement payload = JsonSerializer.SerializeToElement(execution with
            {
                PolicySnapshotId = policySnapshotId,
                ExecutionRecord = record,
            });
            return new EventStoreDispatchPlan(execution.SourceMessageId, commandType, payload);
        }

        if (string.Equals(commandType, nameof(MarkAiActionProposalInvalidatedByCorrection), StringComparison.Ordinal))
        {
            MarkAiActionProposalInvalidatedByCorrection invalidation = command.Deserialize<MarkAiActionProposalInvalidatedByCorrection>(ReadOptions)
                ?? throw new InvalidOperationException("The AI action proposal invalidation command payload could not be read.");
            if (string.IsNullOrWhiteSpace(invalidation.ProposalId) ||
                string.IsNullOrWhiteSpace(invalidation.SourceMessageId) ||
                string.IsNullOrWhiteSpace(invalidation.AssociationId) ||
                string.IsNullOrWhiteSpace(invalidation.CorrectionId))
            {
                throw new InvalidOperationException("The AI action proposal invalidation command is missing correction lineage metadata.");
            }

            JsonElement payload = JsonSerializer.SerializeToElement(invalidation);
            return new EventStoreDispatchPlan(invalidation.SourceMessageId, commandType, payload);
        }

        if (IsAssociationDecisionCommand(commandType))
        {
            EventStoreDispatchPlan? decisionPlan = BuildAssociationDecisionPlan(commandType, command);
            if (decisionPlan is not null)
            {
                return decisionPlan;
            }
        }

        if (string.Equals(commandType, nameof(CorrectEmailProjectAssociation), StringComparison.Ordinal))
        {
            CorrectEmailProjectAssociation payload = command.Deserialize<CorrectEmailProjectAssociation>(ReadOptions)
                ?? throw new InvalidOperationException("The association-correction command payload could not be read.");
            ValidateAssociationDecision(payload.AssociationId, payload.IntakeId, payload.SourceVersion, payload.SchemaVersion);
            if (string.IsNullOrWhiteSpace(payload.PriorProjectId) ||
                string.IsNullOrWhiteSpace(payload.TargetProjectId) ||
                string.IsNullOrWhiteSpace(payload.PredecessorAssociationId) ||
                string.IsNullOrWhiteSpace(payload.CandidateEvidenceFingerprint))
            {
                throw new InvalidOperationException("The association-correction command is missing its correction metadata.");
            }

            long propagationSourceVersion = payload.SourceVersion + 1;
            string correctionId = DaprCorrectionPropagationCoordinator.CorrectionIdFor(payload.AssociationId, propagationSourceVersion);
            string workflowInstanceId = DaprCorrectionPropagationCoordinator.WorkflowInstanceIdFor(
                context.TenantBinding.TenantId,
                payload.AssociationId,
                correctionId,
                propagationSourceVersion);
            CorrectionPropagationRequest propagation = new(
                context.TenantBinding.TenantId,
                context.Actor.ActorId,
                payload.AssociationId,
                payload.IntakeId,
                correctionId,
                workflowInstanceId,
                payload.PriorProjectId,
                payload.TargetProjectId,
                propagationSourceVersion,
                context.Submission.CorrelationId,
                clock.UtcNow,
                clock.UtcNow.Add(DaprCorrectionPropagationCoordinator.M0M1P95Target));

            return new EventStoreDispatchPlan(
                payload.AssociationId,
                commandType,
                JsonSerializer.SerializeToElement(payload),
                propagation);
        }

        // Defensive fallback: the spine allowlist admits only first-party commands in production, so this branch
        // is reached only by bootstrap tests that submit a generic command through a permissive allowlist.
        return new EventStoreDispatchPlan(context.Submission.Request.CommandId, commandType, command);
    }

    private async ValueTask<LowRiskAiAssistanceExecutionRecord> InvokeProviderAsync(
        ChatBotGatewayContext context,
        ExecuteLowRiskAIAssistance execution,
        string policySnapshotId,
        string assistanceKind,
        CancellationToken cancellationToken)
    {
        IAiAssistanceProvider provider = aiAssistanceProvider
            ?? throw new InvalidOperationException("The AI assistance provider is not configured.");
        return await provider
            .ExecuteAsync(
                new AiAssistanceProviderRequest(
                    context.TenantBinding.TenantId,
                    execution.ProjectId,
                    execution.RequesterId,
                    execution.ProposalId,
                    execution.ExecutionId,
                    assistanceKind,
                    execution.ContextPackageId,
                    execution.ContextPackageVersion,
                    execution.ContextPackageRedactionState,
                    execution.RetentionClass,
                    execution.ProviderReuseSetting,
                    execution.SourceEvidenceReferences,
                    execution.AuthorizedContextReferences,
                    execution.ExcludedContextReasons,
                    policySnapshotId,
                    context.ApprovalResult!.ReasonCode,
                    context.Submission.CorrelationId,
                    $"audit:{execution.ExecutionId}"),
                cancellationToken)
            .ConfigureAwait(false);
    }

    private LowRiskAiAssistanceExecutionRecord RoutedToApprovalRecord(
        ChatBotGatewayContext context,
        ExecuteLowRiskAIAssistance execution,
        string policySnapshotId,
        string assistanceKind)
        => new(
            execution.ExecutionId,
            execution.ProposalId,
            assistanceKind,
            "pending-approval",
            "not-invoked",
            "not-invoked",
            clock.UtcNow,
            execution.SourceEvidenceReferences,
            execution.ContextPackageId,
            execution.ContextPackageVersion,
            execution.ContextPackageRedactionState,
            policySnapshotId,
            context.ApprovalResult!.ReasonCode,
            $"audit:{execution.ExecutionId}",
            "available",
            context.Submission.CorrelationId,
            "metadata_only",
            "metadata_only",
            "review-ai-action",
            FailureCode: context.ApprovalResult.ReasonCode,
            Retryability: null,
            RetentionClass: execution.RetentionClass);

    private static JsonElement ToElement(object? command)
        => command is JsonElement element
            ? element
            : JsonSerializer.SerializeToElement(command, ReadOptions);

    private static bool IsAssociationDecisionCommand(string commandType)
        => commandType is nameof(AssociateEmailToProject)
            or nameof(RejectEmailProjectAssociation)
            or nameof(DeferEmailProjectAssociation)
            or nameof(MarkEmailAssociationNeedsReview);

    private static EventStoreDispatchPlan? BuildAssociationDecisionPlan(string commandType, JsonElement command)
    {
        if (string.Equals(commandType, nameof(AssociateEmailToProject), StringComparison.Ordinal))
        {
            AssociateEmailToProject payload = command.Deserialize<AssociateEmailToProject>(ReadOptions)
                ?? throw new InvalidOperationException("The association-decision command payload could not be read.");
            ValidateAssociationDecision(payload.AssociationId, payload.IntakeId, payload.SourceVersion, payload.SchemaVersion);
            if (string.IsNullOrWhiteSpace(payload.ProjectId))
            {
                throw new InvalidOperationException("The association-decision command is missing its selected project identity.");
            }

            return new EventStoreDispatchPlan(payload.AssociationId, commandType, JsonSerializer.SerializeToElement(payload));
        }

        if (string.Equals(commandType, nameof(RejectEmailProjectAssociation), StringComparison.Ordinal))
        {
            RejectEmailProjectAssociation payload = command.Deserialize<RejectEmailProjectAssociation>(ReadOptions)
                ?? throw new InvalidOperationException("The association-decision command payload could not be read.");
            ValidateAssociationDecision(payload.AssociationId, payload.IntakeId, payload.SourceVersion, payload.SchemaVersion);
            return new EventStoreDispatchPlan(payload.AssociationId, commandType, JsonSerializer.SerializeToElement(payload));
        }

        if (string.Equals(commandType, nameof(DeferEmailProjectAssociation), StringComparison.Ordinal))
        {
            DeferEmailProjectAssociation payload = command.Deserialize<DeferEmailProjectAssociation>(ReadOptions)
                ?? throw new InvalidOperationException("The association-decision command payload could not be read.");
            ValidateAssociationDecision(payload.AssociationId, payload.IntakeId, payload.SourceVersion, payload.SchemaVersion);
            return new EventStoreDispatchPlan(payload.AssociationId, commandType, JsonSerializer.SerializeToElement(payload));
        }

        if (string.Equals(commandType, nameof(MarkEmailAssociationNeedsReview), StringComparison.Ordinal))
        {
            MarkEmailAssociationNeedsReview payload = command.Deserialize<MarkEmailAssociationNeedsReview>(ReadOptions)
                ?? throw new InvalidOperationException("The association-decision command payload could not be read.");
            ValidateAssociationDecision(payload.AssociationId, payload.IntakeId, payload.SourceVersion, payload.SchemaVersion);
            return new EventStoreDispatchPlan(payload.AssociationId, commandType, JsonSerializer.SerializeToElement(payload));
        }

        return null;
    }

    private static void ValidateAssociationDecision(
        string associationId,
        string intakeId,
        long sourceVersion,
        string schemaVersion)
    {
        if (!AssociationWorkflowId.TryParse(associationId, out _) ||
            !MailboxMessageIntakeId.TryParse(intakeId, out _) ||
            sourceVersion <= 0 ||
            string.IsNullOrWhiteSpace(schemaVersion))
        {
            throw new InvalidOperationException("The association-decision command is missing its aggregate or source identity.");
        }
    }

    private Dictionary<string, string> BuildExtensions(ChatBotGatewayContext context)
    {
        Dictionary<string, string> extensions = new(StringComparer.Ordinal)
        {
            ["surfaceOrigin"] = ChatBotSurfaceOrigins.ToWireValue(context.Submission.Origin),
            ["decidedAt"] = clock.UtcNow.ToString("O", System.Globalization.CultureInfo.InvariantCulture),
        };

        string? actorType = context.Actor.Principal.Claims
            .FirstOrDefault(static claim => string.Equals(claim.Type, "actor_type", StringComparison.Ordinal))?
            .Value;
        if (!string.IsNullOrWhiteSpace(actorType))
        {
            extensions["actorType"] = actorType;
        }

        if (!string.IsNullOrWhiteSpace(context.Submission.TaskId))
        {
            extensions["taskId"] = context.Submission.TaskId;
        }

        return extensions;
    }

    private sealed record EventStoreDispatchPlan(
        string AggregateId,
        string CommandType,
        JsonElement Payload,
        CorrectionPropagationRequest? CorrectionPropagation = null);
}
