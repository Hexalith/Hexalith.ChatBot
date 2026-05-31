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
