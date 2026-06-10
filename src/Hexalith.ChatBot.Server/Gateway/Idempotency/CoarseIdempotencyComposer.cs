using System.Text;
using System.Text.Json;

using Hexalith.ChatBot.Contracts.Commands;
using Hexalith.ChatBot.Server.Association.Scoring;
using Hexalith.ChatBot.Server.Audit;

namespace Hexalith.ChatBot.Server.Gateway.Idempotency;

internal static class CoarseIdempotencyComposer
{
    public static CoarseIdempotencyRecord ComposeCommandExecutionRecord(
        ChatBotGatewayContext context,
        DateTimeOffset now)
    {
        ArgumentNullException.ThrowIfNull(context);

        if (IsMailboxIntake(context))
        {
            return ComposeMessageIntakeRecord(context, now);
        }

        if (IsParticipantResolution(context))
        {
            return ComposeParticipantResolutionRecord(context, now);
        }

        if (IsAssociationScoring(context))
        {
            return ComposeAssociationScoringRecord(context, now);
        }

        if (IsAssociationThresholdPolicy(context))
        {
            return ComposeAssociationThresholdPolicyRecord(context, now);
        }

        if (IsAssociationDecision(context))
        {
            return ComposeAssociationDecisionRecord(context, now);
        }

        if (IsAssociationCorrection(context))
        {
            return ComposeAssociationCorrectionRecord(context, now);
        }

        if (IsRetry(context))
        {
            return ComposeRetryRecord(context, now);
        }

        if (IsLowRiskAiAssistance(context))
        {
            return ComposeLowRiskAiAssistanceRecord(context, now);
        }

        if (IsApprovalDecision(context))
        {
            return ComposeApprovalDecisionRecord(context, now);
        }

        if (IsOutboundApprovalDecision(context))
        {
            return ComposeOutboundApprovalDecisionRecord(context, now);
        }

        if (IsApprovedAiActionExecution(context))
        {
            return ComposeApprovedAiActionExecutionRecord(context, now);
        }

        if (IsOutboundDraftCreation(context))
        {
            return ComposeOutboundDraftCreationRecord(context, now);
        }

        if (IsOutboundSend(context))
        {
            return ComposeOutboundSendRecord(context, now);
        }

        CoarseIdempotencyOperationClass operation = CoarseIdempotencyOperationClass.CommandExecution;
        string commandName = AuditMetadata.SafeCommandName(context.Submission.Request.CommandType);
        string commandInputHash = HashCommandInput(context.Submission.Request.Command);
        string coarseKeyHash = HashParts(
            context.TenantBinding.TenantId,
            operation.Code,
            commandName,
            commandInputHash,
            context.Actor.ActorId);
        DateTimeOffset expiresAt = operation.ReplayWindow is { } replayWindow
            ? now.Add(replayWindow)
            : DateTimeOffset.MaxValue;

        return new CoarseIdempotencyRecord(
            context.TenantBinding.TenantId,
            operation.Code,
            coarseKeyHash,
            commandInputHash,
            context.Submission.CorrelationId,
            context.Submission.TaskId,
            context.Submission.Request.CommandId,
            commandName,
            context.Actor.ActorId,
            now,
            expiresAt,
            PriorOutcome: null);
    }

    private static CoarseIdempotencyRecord ComposeMessageIntakeRecord(ChatBotGatewayContext context, DateTimeOffset now)
    {
        CaptureMailboxMessageIntake command = ReadMailboxIntake(context);
        CoarseIdempotencyOperationClass operation = CoarseIdempotencyOperationClass.MessageIntake;
        string commandName = AuditMetadata.SafeCommandName(context.Submission.Request.CommandType);
        string coarseKeyHash = HashParts(
            context.TenantBinding.TenantId,
            command.Source.MailboxId,
            command.Source.ProviderMessageId);
        DateTimeOffset expiresAt = operation.ReplayWindow is { } replayWindow
            ? now.Add(replayWindow)
            : DateTimeOffset.MaxValue;

        return new CoarseIdempotencyRecord(
            context.TenantBinding.TenantId,
            operation.Code,
            coarseKeyHash,
            coarseKeyHash,
            context.Submission.CorrelationId,
            context.Submission.TaskId,
            context.Submission.Request.CommandId,
            commandName,
            context.Actor.ActorId,
            now,
            expiresAt,
            PriorOutcome: null);
    }

    private static bool IsMailboxIntake(ChatBotGatewayContext context)
        => string.Equals(context.Submission.Request.CommandType, nameof(CaptureMailboxMessageIntake), StringComparison.Ordinal);

    private static CoarseIdempotencyRecord ComposeParticipantResolutionRecord(ChatBotGatewayContext context, DateTimeOffset now)
    {
        ResolveMailboxMessageParticipants command = ReadParticipantResolution(context);
        CoarseIdempotencyOperationClass operation = CoarseIdempotencyOperationClass.ParticipantResolution;
        string commandName = AuditMetadata.SafeCommandName(context.Submission.Request.CommandType);
        string participantFingerprint = string.Join(
            ',',
            (command.SourceParticipants ?? Array.Empty<MailboxParticipantSourceReference>())
                .Select(static source => (source.EvidenceFingerprint ?? string.Empty).Normalize(NormalizationForm.FormC))
                .Order(StringComparer.Ordinal));
        string coarseKeyHash = HashParts(
            context.TenantBinding.TenantId,
            command.IntakeId ?? string.Empty,
            participantFingerprint,
            command.ResolutionKernelVersion ?? string.Empty);

        return new CoarseIdempotencyRecord(
            context.TenantBinding.TenantId,
            operation.Code,
            coarseKeyHash,
            coarseKeyHash,
            context.Submission.CorrelationId,
            context.Submission.TaskId,
            context.Submission.Request.CommandId,
            commandName,
            context.Actor.ActorId,
            now,
            DateTimeOffset.MaxValue,
            PriorOutcome: null);
    }

    private static bool IsParticipantResolution(ChatBotGatewayContext context)
        => string.Equals(context.Submission.Request.CommandType, nameof(ResolveMailboxMessageParticipants), StringComparison.Ordinal);

    private static CoarseIdempotencyRecord ComposeAssociationScoringRecord(ChatBotGatewayContext context, DateTimeOffset now)
    {
        ScoreMailboxMessageAssociation command = ReadAssociationScoring(context);
        CoarseIdempotencyOperationClass operation = CoarseIdempotencyOperationClass.AssociationScoring;
        string commandName = AuditMetadata.SafeCommandName(context.Submission.Request.CommandType);
        string signalFingerprint = string.Join(
            ',',
            (command.DeterministicSignals ?? Array.Empty<AssociationDeterministicSignal>())
                .Select(static signal => (signal.EvidenceFingerprint ?? string.Empty).Normalize(NormalizationForm.FormC))
                .Order(StringComparer.Ordinal));
        string coarseKeyHash = HashParts(
            context.TenantBinding.TenantId,
            command.IntakeId ?? string.Empty,
            AssociationKernelVersionOrDefault(command.ScoringKernelVersion),
            signalFingerprint);

        return new CoarseIdempotencyRecord(
            context.TenantBinding.TenantId,
            operation.Code,
            coarseKeyHash,
            coarseKeyHash,
            context.Submission.CorrelationId,
            context.Submission.TaskId,
            context.Submission.Request.CommandId,
            commandName,
            context.Actor.ActorId,
            now,
            DateTimeOffset.MaxValue,
            PriorOutcome: null);
    }

    private static bool IsAssociationScoring(ChatBotGatewayContext context)
        => string.Equals(context.Submission.Request.CommandType, nameof(ScoreMailboxMessageAssociation), StringComparison.Ordinal);

    private static CoarseIdempotencyRecord ComposeAssociationThresholdPolicyRecord(ChatBotGatewayContext context, DateTimeOffset now)
    {
        SetAssociationConfidenceThresholds command = ReadAssociationThresholdPolicy(context);
        CoarseIdempotencyOperationClass operation = CoarseIdempotencyOperationClass.AssociationThresholdPolicy;
        string commandName = AuditMetadata.SafeCommandName(context.Submission.Request.CommandType);
        string coarseKeyHash = HashParts(
            context.TenantBinding.TenantId,
            command.PolicyId ?? string.Empty,
            command.PolicyVersion ?? string.Empty);

        return new CoarseIdempotencyRecord(
            context.TenantBinding.TenantId,
            operation.Code,
            coarseKeyHash,
            coarseKeyHash,
            context.Submission.CorrelationId,
            context.Submission.TaskId,
            context.Submission.Request.CommandId,
            commandName,
            context.Actor.ActorId,
            now,
            DateTimeOffset.MaxValue,
            PriorOutcome: null);
    }

    private static bool IsAssociationThresholdPolicy(ChatBotGatewayContext context)
        => string.Equals(context.Submission.Request.CommandType, nameof(SetAssociationConfidenceThresholds), StringComparison.Ordinal);

    private static CoarseIdempotencyRecord ComposeAssociationDecisionRecord(ChatBotGatewayContext context, DateTimeOffset now)
    {
        (string AssociationId, string IntakeId, string DecisionKind, string? ProjectId, string EvidenceFingerprint, long SourceVersion, string SchemaVersion) command = ReadAssociationDecision(context);
        CoarseIdempotencyOperationClass operation = CoarseIdempotencyOperationClass.AssociationDecision;
        string commandName = AuditMetadata.SafeCommandName(context.Submission.Request.CommandType);
        string coarseKeyHash = HashParts(
            context.TenantBinding.TenantId,
            command.IntakeId,
            context.Actor.ActorId,
            command.DecisionKind);
        string equivalenceHash = HashParts(
            context.TenantBinding.TenantId,
            command.IntakeId,
            context.Actor.ActorId,
            command.DecisionKind,
            command.AssociationId,
            command.ProjectId ?? string.Empty,
            command.EvidenceFingerprint,
            command.SourceVersion.ToString(System.Globalization.CultureInfo.InvariantCulture),
            command.SchemaVersion);
        DateTimeOffset expiresAt = operation.ReplayWindow is { } replayWindow
            ? now.Add(replayWindow)
            : DateTimeOffset.MaxValue;

        return new CoarseIdempotencyRecord(
            context.TenantBinding.TenantId,
            operation.Code,
            coarseKeyHash,
            equivalenceHash,
            context.Submission.CorrelationId,
            context.Submission.TaskId,
            context.Submission.Request.CommandId,
            commandName,
            context.Actor.ActorId,
            now,
            expiresAt,
            PriorOutcome: null);
    }

    private static bool IsAssociationDecision(ChatBotGatewayContext context)
        => context.Submission.Request.CommandType is nameof(AssociateEmailToProject)
            or nameof(RejectEmailProjectAssociation)
            or nameof(DeferEmailProjectAssociation)
            or nameof(MarkEmailAssociationNeedsReview);

    private static CoarseIdempotencyRecord ComposeAssociationCorrectionRecord(ChatBotGatewayContext context, DateTimeOffset now)
    {
        CorrectEmailProjectAssociation command = ReadAssociationCorrection(context);
        CoarseIdempotencyOperationClass operation = CoarseIdempotencyOperationClass.Correction;
        string commandName = AuditMetadata.SafeCommandName(context.Submission.Request.CommandType);
        string correctionKind = command.CorrectionKind.ToString();
        string coarseKeyHash = HashParts(
            context.TenantBinding.TenantId,
            command.IntakeId,
            context.Actor.ActorId,
            correctionKind);
        string equivalenceHash = HashParts(
            context.TenantBinding.TenantId,
            command.IntakeId,
            context.Actor.ActorId,
            correctionKind,
            command.AssociationId,
            command.PriorProjectId,
            command.TargetProjectId,
            command.PredecessorAssociationId,
            command.CandidateEvidenceFingerprint,
            command.SourceVersion.ToString(System.Globalization.CultureInfo.InvariantCulture),
            command.SchemaVersion);

        return new CoarseIdempotencyRecord(
            context.TenantBinding.TenantId,
            operation.Code,
            coarseKeyHash,
            equivalenceHash,
            context.Submission.CorrelationId,
            context.Submission.TaskId,
            context.Submission.Request.CommandId,
            commandName,
            context.Actor.ActorId,
            now,
            DateTimeOffset.MaxValue,
            PriorOutcome: null);
    }

    private static bool IsAssociationCorrection(ChatBotGatewayContext context)
        => string.Equals(context.Submission.Request.CommandType, nameof(CorrectEmailProjectAssociation), StringComparison.Ordinal);

    private static CoarseIdempotencyRecord ComposeRetryRecord(ChatBotGatewayContext context, DateTimeOffset now)
    {
        RequestFailedWorkflowRetry command = ReadRetry(context);
        CoarseIdempotencyOperationClass operation = CoarseIdempotencyOperationClass.Retry;
        string commandName = AuditMetadata.SafeCommandName(context.Submission.Request.CommandType);
        string coarseKeyHash = HashParts(
            context.TenantBinding.TenantId,
            command.FailedEventId,
            context.Actor.ActorId);
        string equivalenceHash = HashParts(
            context.TenantBinding.TenantId,
            command.FailedEventId,
            context.Actor.ActorId,
            command.FailedOperationClass,
            command.FailureReasonCode,
            command.ExpectedFailedSourceVersion.ToString(System.Globalization.CultureInfo.InvariantCulture));

        return new CoarseIdempotencyRecord(
            context.TenantBinding.TenantId,
            operation.Code,
            coarseKeyHash,
            equivalenceHash,
            context.Submission.CorrelationId,
            context.Submission.TaskId,
            context.Submission.Request.CommandId,
            commandName,
            context.Actor.ActorId,
            now,
            DateTimeOffset.MaxValue,
            PriorOutcome: null);
    }

    private static bool IsRetry(ChatBotGatewayContext context)
        => string.Equals(context.Submission.Request.CommandType, nameof(RequestFailedWorkflowRetry), StringComparison.Ordinal);

    private static CoarseIdempotencyRecord ComposeLowRiskAiAssistanceRecord(ChatBotGatewayContext context, DateTimeOffset now)
    {
        ExecuteLowRiskAIAssistance command = ReadLowRiskAiAssistance(context);
        CoarseIdempotencyOperationClass operation = CoarseIdempotencyOperationClass.LowRiskAiAssistance;
        string commandName = AuditMetadata.SafeCommandName(context.Submission.Request.CommandType);
        string coarseKeyHash = HashParts(
            context.TenantBinding.TenantId,
            command.ProposalId,
            command.ContextPackageId,
            command.ContextPackageVersion);
        string equivalenceHash = HashParts(
            context.TenantBinding.TenantId,
            command.ProjectId,
            command.ProposalId,
            command.TaskIntentId,
            command.SourceMessageId,
            command.RequesterId,
            command.AssistanceKind.ToString(),
            command.ContextPackageId,
            command.ContextPackageVersion,
            command.ExpectedProposalSourceVersion.ToString(System.Globalization.CultureInfo.InvariantCulture));
        DateTimeOffset expiresAt = operation.ReplayWindow is { } replayWindow
            ? now.Add(replayWindow)
            : DateTimeOffset.MaxValue;

        return new CoarseIdempotencyRecord(
            context.TenantBinding.TenantId,
            operation.Code,
            coarseKeyHash,
            equivalenceHash,
            context.Submission.CorrelationId,
            context.Submission.TaskId,
            context.Submission.Request.CommandId,
            commandName,
            context.Actor.ActorId,
            now,
            expiresAt,
            PriorOutcome: null);
    }

    private static bool IsLowRiskAiAssistance(ChatBotGatewayContext context)
        => string.Equals(context.Submission.Request.CommandType, nameof(ExecuteLowRiskAIAssistance), StringComparison.Ordinal);

    private static CoarseIdempotencyRecord ComposeApprovalDecisionRecord(ChatBotGatewayContext context, DateTimeOffset now)
    {
        DecideAiActionApproval command = ReadApprovalDecision(context);
        CoarseIdempotencyOperationClass operation = CoarseIdempotencyOperationClass.ApprovalDecision;
        string commandName = AuditMetadata.SafeCommandName(context.Submission.Request.CommandType);
        string coarseKeyHash = HashParts(
            context.TenantBinding.TenantId,
            command.ApprovalId,
            context.Actor.ActorId,
            command.Decision.ToString());
        string equivalenceHash = HashParts(
            context.TenantBinding.TenantId,
            command.ApprovalId,
            context.Actor.ActorId,
            command.Decision.ToString(),
            command.ProposalId,
            command.SourceMessageId,
            command.ExpectedApprovalSourceVersion.ToString(System.Globalization.CultureInfo.InvariantCulture),
            command.RationaleRedactionState,
            command.SchemaVersion);
        DateTimeOffset expiresAt = operation.ReplayWindow is { } replayWindow
            ? now.Add(replayWindow)
            : DateTimeOffset.MaxValue;

        return new CoarseIdempotencyRecord(
            context.TenantBinding.TenantId,
            operation.Code,
            coarseKeyHash,
            equivalenceHash,
            context.Submission.CorrelationId,
            context.Submission.TaskId,
            context.Submission.Request.CommandId,
            commandName,
            context.Actor.ActorId,
            now,
            expiresAt,
            PriorOutcome: null);
    }

    private static bool IsApprovalDecision(ChatBotGatewayContext context)
        => string.Equals(context.Submission.Request.CommandType, nameof(DecideAiActionApproval), StringComparison.Ordinal);

    private static CoarseIdempotencyRecord ComposeOutboundApprovalDecisionRecord(ChatBotGatewayContext context, DateTimeOffset now)
    {
        DecideOutboundApproval command = ReadOutboundApprovalDecision(context);
        CoarseIdempotencyOperationClass operation = CoarseIdempotencyOperationClass.ApprovalDecision;
        string commandName = AuditMetadata.SafeCommandName(context.Submission.Request.CommandType);
        string coarseKeyHash = HashParts(
            context.TenantBinding.TenantId,
            command.ApprovalId,
            context.Actor.ActorId,
            command.Decision.ToString());
        string equivalenceHash = HashParts(
            context.TenantBinding.TenantId,
            command.ApprovalId,
            context.Actor.ActorId,
            command.Decision.ToString(),
            command.DraftId,
            command.ProjectId,
            command.ExpectedApprovalSourceVersion.ToString(System.Globalization.CultureInfo.InvariantCulture),
            command.DecisionRationaleRedactionState,
            command.SchemaVersion);
        DateTimeOffset expiresAt = operation.ReplayWindow is { } replayWindow
            ? now.Add(replayWindow)
            : DateTimeOffset.MaxValue;

        return new CoarseIdempotencyRecord(
            context.TenantBinding.TenantId,
            operation.Code,
            coarseKeyHash,
            equivalenceHash,
            context.Submission.CorrelationId,
            context.Submission.TaskId,
            context.Submission.Request.CommandId,
            commandName,
            context.Actor.ActorId,
            now,
            expiresAt,
            PriorOutcome: null);
    }

    private static bool IsOutboundApprovalDecision(ChatBotGatewayContext context)
        => string.Equals(context.Submission.Request.CommandType, nameof(DecideOutboundApproval), StringComparison.Ordinal);

    private static CoarseIdempotencyRecord ComposeApprovedAiActionExecutionRecord(ChatBotGatewayContext context, DateTimeOffset now)
    {
        ExecuteApprovedAIAction command = ReadApprovedAiActionExecution(context);
        CoarseIdempotencyOperationClass operation = CoarseIdempotencyOperationClass.ApprovedAiActionExecution;
        string commandName = AuditMetadata.SafeCommandName(context.Submission.Request.CommandType);
        string commandInputHash = HashCommandInput(context.Submission.Request.Command);
        string coarseKeyHash = HashParts(
            context.TenantBinding.TenantId,
            command.CommandName,
            commandInputHash,
            command.RequesterId);
        DateTimeOffset expiresAt = operation.ReplayWindow is { } replayWindow
            ? now.Add(replayWindow)
            : DateTimeOffset.MaxValue;

        return new CoarseIdempotencyRecord(
            context.TenantBinding.TenantId,
            operation.Code,
            coarseKeyHash,
            commandInputHash,
            context.Submission.CorrelationId,
            context.Submission.TaskId,
            context.Submission.Request.CommandId,
            commandName,
            context.Actor.ActorId,
            now,
            expiresAt,
            PriorOutcome: null);
    }

    private static bool IsApprovedAiActionExecution(ChatBotGatewayContext context)
        => string.Equals(context.Submission.Request.CommandType, nameof(ExecuteApprovedAIAction), StringComparison.Ordinal);

    private static CoarseIdempotencyRecord ComposeOutboundDraftCreationRecord(ChatBotGatewayContext context, DateTimeOffset now)
    {
        CreateOutboundDraft command = ReadOutboundDraftCreation(context);
        CoarseIdempotencyOperationClass operation = CoarseIdempotencyOperationClass.OutboundDraftCreation;
        string commandName = AuditMetadata.SafeCommandName(context.Submission.Request.CommandType);
        string equivalenceHash = HashParts(
            context.TenantBinding.TenantId,
            command.ProjectId,
            command.RequesterId,
            command.DraftId,
            command.SourceActorId,
            command.SourceConversationId ?? string.Empty,
            command.SourceMessageId ?? string.Empty,
            command.SourceConversationItemId ?? string.Empty,
            string.Join(',', command.RecipientRefs.Order(StringComparer.Ordinal)),
            string.Join(',', command.ContextRefs.Order(StringComparer.Ordinal)),
            command.PolicySnapshotId,
            command.SenderAuthorityClass.ToString(),
            command.HasM365SendPosture.ToString(System.Globalization.CultureInfo.InvariantCulture),
            command.GovernedContent.Subject,
            command.GovernedContent.ContentText,
            command.GovernedContent.ContentFormat,
            command.RedactionState,
            command.RetentionClass,
            command.SchemaVersion);
        string coarseKeyHash = HashParts(
            context.TenantBinding.TenantId,
            operation.Code,
            command.ProjectId,
            command.RequesterId,
            command.DraftId);

        return new CoarseIdempotencyRecord(
            context.TenantBinding.TenantId,
            operation.Code,
            coarseKeyHash,
            equivalenceHash,
            context.Submission.CorrelationId,
            context.Submission.TaskId,
            context.Submission.Request.CommandId,
            commandName,
            context.Actor.ActorId,
            now,
            DateTimeOffset.MaxValue,
            PriorOutcome: null);
    }

    private static bool IsOutboundDraftCreation(ChatBotGatewayContext context)
        => string.Equals(context.Submission.Request.CommandType, nameof(CreateOutboundDraft), StringComparison.Ordinal);

    private static CoarseIdempotencyRecord ComposeOutboundSendRecord(ChatBotGatewayContext context, DateTimeOffset now)
    {
        ExecuteApprovedOutboundDraft command = ReadOutboundSend(context);
        CoarseIdempotencyOperationClass operation = CoarseIdempotencyOperationClass.OutboundSend;
        string commandName = AuditMetadata.SafeCommandName(context.Submission.Request.CommandType);
        string coarseKeyHash = HashParts(
            context.TenantBinding.TenantId,
            command.DraftId,
            command.SendActorId);
        string equivalenceHash = HashParts(
            context.TenantBinding.TenantId,
            command.DraftId,
            command.SendActorId,
            command.ApprovalId,
            command.ProjectId,
            command.RequesterId,
            command.ExpectedApprovalSourceVersion.ToString(System.Globalization.CultureInfo.InvariantCulture),
            command.ExpectedDraftSourceVersion.ToString(System.Globalization.CultureInfo.InvariantCulture),
            command.AdapterMode);

        return new CoarseIdempotencyRecord(
            context.TenantBinding.TenantId,
            operation.Code,
            coarseKeyHash,
            equivalenceHash,
            context.Submission.CorrelationId,
            context.Submission.TaskId,
            context.Submission.Request.CommandId,
            commandName,
            context.Actor.ActorId,
            now,
            DateTimeOffset.MaxValue,
            PriorOutcome: null);
    }

    private static bool IsOutboundSend(ChatBotGatewayContext context)
        => string.Equals(context.Submission.Request.CommandType, nameof(ExecuteApprovedOutboundDraft), StringComparison.Ordinal);

    private static CaptureMailboxMessageIntake ReadMailboxIntake(ChatBotGatewayContext context)
    {
        if (context.Submission.Request.Command is CaptureMailboxMessageIntake typed)
        {
            return typed;
        }

        JsonElement element = context.Submission.Request.Command is JsonElement jsonElement
            ? jsonElement
            : JsonSerializer.SerializeToElement(context.Submission.Request.Command, JsonOptions);

        return element.Deserialize<CaptureMailboxMessageIntake>(JsonOptions)
            ?? throw new InvalidOperationException("The mailbox-intake command payload could not be read.");
    }

    private static ResolveMailboxMessageParticipants ReadParticipantResolution(ChatBotGatewayContext context)
    {
        if (context.Submission.Request.Command is ResolveMailboxMessageParticipants typed)
        {
            return typed;
        }

        JsonElement element = context.Submission.Request.Command is JsonElement jsonElement
            ? jsonElement
            : JsonSerializer.SerializeToElement(context.Submission.Request.Command, JsonOptions);

        return element.Deserialize<ResolveMailboxMessageParticipants>(JsonOptions)
            ?? throw new InvalidOperationException("The participant-resolution command payload could not be read.");
    }

    private static ScoreMailboxMessageAssociation ReadAssociationScoring(ChatBotGatewayContext context)
    {
        if (context.Submission.Request.Command is ScoreMailboxMessageAssociation typed)
        {
            return typed;
        }

        JsonElement element = context.Submission.Request.Command is JsonElement jsonElement
            ? jsonElement
            : JsonSerializer.SerializeToElement(context.Submission.Request.Command, JsonOptions);

        return element.Deserialize<ScoreMailboxMessageAssociation>(JsonOptions)
            ?? throw new InvalidOperationException("The association-scoring command payload could not be read.");
    }

    private static SetAssociationConfidenceThresholds ReadAssociationThresholdPolicy(ChatBotGatewayContext context)
    {
        if (context.Submission.Request.Command is SetAssociationConfidenceThresholds typed)
        {
            return typed;
        }

        JsonElement element = context.Submission.Request.Command is JsonElement jsonElement
            ? jsonElement
            : JsonSerializer.SerializeToElement(context.Submission.Request.Command, JsonOptions);

        return element.Deserialize<SetAssociationConfidenceThresholds>(JsonOptions)
            ?? throw new InvalidOperationException("The association-threshold command payload could not be read.");
    }

    private static (string AssociationId, string IntakeId, string DecisionKind, string? ProjectId, string EvidenceFingerprint, long SourceVersion, string SchemaVersion) ReadAssociationDecision(ChatBotGatewayContext context)
    {
        JsonElement element = context.Submission.Request.Command is JsonElement jsonElement
            ? jsonElement
            : JsonSerializer.SerializeToElement(context.Submission.Request.Command, JsonOptions);

        string commandType = context.Submission.Request.CommandType ?? string.Empty;
        if (string.Equals(commandType, nameof(AssociateEmailToProject), StringComparison.Ordinal))
        {
            AssociateEmailToProject command = element.Deserialize<AssociateEmailToProject>(JsonOptions)
                ?? throw new InvalidOperationException("The association-decision command payload could not be read.");
            return (command.AssociationId, command.IntakeId, command.DecisionKind.ToString(), command.ProjectId, command.CandidateEvidenceFingerprint, command.SourceVersion, command.SchemaVersion);
        }

        if (string.Equals(commandType, nameof(RejectEmailProjectAssociation), StringComparison.Ordinal))
        {
            RejectEmailProjectAssociation command = element.Deserialize<RejectEmailProjectAssociation>(JsonOptions)
                ?? throw new InvalidOperationException("The association-decision command payload could not be read.");
            return (command.AssociationId, command.IntakeId, command.DecisionKind.ToString(), null, command.CandidateEvidenceFingerprint, command.SourceVersion, command.SchemaVersion);
        }

        if (string.Equals(commandType, nameof(DeferEmailProjectAssociation), StringComparison.Ordinal))
        {
            DeferEmailProjectAssociation command = element.Deserialize<DeferEmailProjectAssociation>(JsonOptions)
                ?? throw new InvalidOperationException("The association-decision command payload could not be read.");
            return (command.AssociationId, command.IntakeId, command.DecisionKind.ToString(), null, command.CandidateEvidenceFingerprint, command.SourceVersion, command.SchemaVersion);
        }

        MarkEmailAssociationNeedsReview needsReview = element.Deserialize<MarkEmailAssociationNeedsReview>(JsonOptions)
            ?? throw new InvalidOperationException("The association-decision command payload could not be read.");
        return (needsReview.AssociationId, needsReview.IntakeId, needsReview.DecisionKind.ToString(), null, needsReview.CandidateEvidenceFingerprint, needsReview.SourceVersion, needsReview.SchemaVersion);
    }

    private static CorrectEmailProjectAssociation ReadAssociationCorrection(ChatBotGatewayContext context)
    {
        if (context.Submission.Request.Command is CorrectEmailProjectAssociation typed)
        {
            return typed;
        }

        JsonElement element = context.Submission.Request.Command is JsonElement jsonElement
            ? jsonElement
            : JsonSerializer.SerializeToElement(context.Submission.Request.Command, JsonOptions);

        return element.Deserialize<CorrectEmailProjectAssociation>(JsonOptions)
            ?? throw new InvalidOperationException("The association-correction command payload could not be read.");
    }

    private static RequestFailedWorkflowRetry ReadRetry(ChatBotGatewayContext context)
    {
        if (context.Submission.Request.Command is RequestFailedWorkflowRetry typed)
        {
            return typed;
        }

        JsonElement element = context.Submission.Request.Command is JsonElement jsonElement
            ? jsonElement
            : JsonSerializer.SerializeToElement(context.Submission.Request.Command, JsonOptions);

        return element.Deserialize<RequestFailedWorkflowRetry>(JsonOptions)
            ?? throw new InvalidOperationException("The retry command payload could not be read.");
    }

    private static ExecuteLowRiskAIAssistance ReadLowRiskAiAssistance(ChatBotGatewayContext context)
    {
        if (context.Submission.Request.Command is ExecuteLowRiskAIAssistance typed)
        {
            return typed;
        }

        JsonElement element = context.Submission.Request.Command is JsonElement jsonElement
            ? jsonElement
            : JsonSerializer.SerializeToElement(context.Submission.Request.Command, JsonOptions);

        return element.Deserialize<ExecuteLowRiskAIAssistance>(JsonOptions)
            ?? throw new InvalidOperationException("The low-risk AI assistance execution command payload could not be read.");
    }

    private static DecideAiActionApproval ReadApprovalDecision(ChatBotGatewayContext context)
    {
        if (context.Submission.Request.Command is DecideAiActionApproval typed)
        {
            return typed;
        }

        JsonElement element = context.Submission.Request.Command is JsonElement jsonElement
            ? jsonElement
            : JsonSerializer.SerializeToElement(context.Submission.Request.Command, JsonOptions);

        return element.Deserialize<DecideAiActionApproval>(JsonOptions)
            ?? throw new InvalidOperationException("The AI action approval decision command payload could not be read.");
    }

    private static ExecuteApprovedAIAction ReadApprovedAiActionExecution(ChatBotGatewayContext context)
    {
        if (context.Submission.Request.Command is ExecuteApprovedAIAction typed)
        {
            return typed;
        }

        JsonElement element = context.Submission.Request.Command is JsonElement jsonElement
            ? jsonElement
            : JsonSerializer.SerializeToElement(context.Submission.Request.Command, JsonOptions);

        return element.Deserialize<ExecuteApprovedAIAction>(JsonOptions)
            ?? throw new InvalidOperationException("The approved AI action execution command payload could not be read.");
    }

    private static DecideOutboundApproval ReadOutboundApprovalDecision(ChatBotGatewayContext context)
    {
        if (context.Submission.Request.Command is DecideOutboundApproval typed)
        {
            return typed;
        }

        JsonElement element = context.Submission.Request.Command is JsonElement jsonElement
            ? jsonElement
            : JsonSerializer.SerializeToElement(context.Submission.Request.Command, JsonOptions);

        return element.Deserialize<DecideOutboundApproval>(JsonOptions)
            ?? throw new InvalidOperationException("The outbound approval decision command payload could not be read.");
    }

    private static CreateOutboundDraft ReadOutboundDraftCreation(ChatBotGatewayContext context)
    {
        if (context.Submission.Request.Command is CreateOutboundDraft typed)
        {
            return typed;
        }

        JsonElement element = context.Submission.Request.Command is JsonElement jsonElement
            ? jsonElement
            : JsonSerializer.SerializeToElement(context.Submission.Request.Command, JsonOptions);

        return element.Deserialize<CreateOutboundDraft>(JsonOptions)
            ?? throw new InvalidOperationException("The outbound draft creation command payload could not be read.");
    }

    private static ExecuteApprovedOutboundDraft ReadOutboundSend(ChatBotGatewayContext context)
    {
        if (context.Submission.Request.Command is ExecuteApprovedOutboundDraft typed)
        {
            return typed;
        }

        JsonElement element = context.Submission.Request.Command is JsonElement jsonElement
            ? jsonElement
            : JsonSerializer.SerializeToElement(context.Submission.Request.Command, JsonOptions);

        return element.Deserialize<ExecuteApprovedOutboundDraft>(JsonOptions)
            ?? throw new InvalidOperationException("The outbound send command payload could not be read.");
    }

    private static string HashCommandInput(object? command)
    {
        using JsonDocument document = JsonDocument.Parse(JsonSerializer.Serialize(command, JsonOptions));
        return CoarseIdempotencyCanonicalizer.HashCanonicalJson(document.RootElement);
    }

    private static string AssociationKernelVersionOrDefault(string? kernelVersion)
        => string.IsNullOrWhiteSpace(kernelVersion)
            ? DeterministicAssociationScorer.CurrentKernelVersion
            : kernelVersion;

    private static string HashParts(params string[] parts)
    {
        string value = string.Join('\u001f', parts.Select(static part => part.Normalize(NormalizationForm.FormC)));
        return CoarseIdempotencyCanonicalizer.HashUtf8(value);
    }

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
}
