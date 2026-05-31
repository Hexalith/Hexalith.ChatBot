using System.Text.Json;

using Hexalith.ChatBot.Contracts.Commands;
using Hexalith.ChatBot.Contracts.Enums;
using Hexalith.ChatBot.Contracts.Identities;
using Hexalith.ChatBot.Server.Audit;
using Hexalith.ChatBot.Server.Gateway;
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
    ISystemClock clock) : ICommandDispatcher
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

        if (IsAssociationDecisionCommand(commandType))
        {
            EventStoreDispatchPlan? decisionPlan = BuildAssociationDecisionPlan(commandType, command);
            if (decisionPlan is not null)
            {
                return decisionPlan;
            }
        }

        // Defensive fallback: the spine allowlist admits only first-party commands in production, so this branch
        // is reached only by bootstrap tests that submit a generic command through a permissive allowlist.
        return new EventStoreDispatchPlan(context.Submission.Request.CommandId, commandType, command);
    }

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

    private sealed record EventStoreDispatchPlan(string AggregateId, string CommandType, JsonElement Payload);
}
