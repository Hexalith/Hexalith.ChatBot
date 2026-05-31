using System.Text.Json;

using Hexalith.ChatBot.Contracts.Commands;
using Hexalith.ChatBot.Contracts.Enums;
using Hexalith.ChatBot.Server.Gateway;
using Hexalith.ChatBot.Server.Gateway.Redaction;
using Hexalith.ChatBot.Server.Lifecycle.StateModel;

namespace Hexalith.ChatBot.Server.Audit;

internal static class AuditEnvelopeFactory
{
    private const string EnvelopeSchemaVersion = "chatbot.audit-envelope.v1";
    private const string NoPayloadPolicySnapshotId = "chatbot.gateway.policy-snapshot.v1";

    public static AuditEnvelope PreCommit(
        ChatBotGatewayContext context,
        LifecycleTransitionDefinition transition,
        DateTimeOffset timestamp)
        => Create(
            context,
            timestamp,
            AuditCommitPhase.PreCommit,
            decision: "allow",
            reasonCode: "pre_commit_gate",
            stateTransition: transition.ToString(),
            outcome: "gate_passed");

    public static AuditEnvelope PostCommit(
        ChatBotGatewayContext context,
        ChatBotDispatchResult dispatchResult,
        LifecycleTransitionDefinition transition,
        DateTimeOffset timestamp)
    {
        ArgumentNullException.ThrowIfNull(dispatchResult);

        return Create(
            context,
            timestamp,
            AuditCommitPhase.PostCommit,
            decision: "allow",
            reasonCode: "eventstore_dispatch_accepted",
            stateTransition: transition.ToString(),
            outcome: "proposed",
            resourceId: dispatchResult.ResourceId);
    }

    public static AuditEnvelope DuplicateMailboxIntakeSuppressed(ChatBotGatewayContext context, DateTimeOffset timestamp)
        => Create(
            context,
            timestamp,
            AuditCommitPhase.PostCommit,
            decision: "suppress",
            reasonCode: "duplicate_provider_message",
            stateTransition: "Received->Skipped",
            outcome: "duplicate_suppressed");

    public static AuditEnvelope RejectedLifecycleTransition(
        ChatBotGatewayContext context,
        LifecycleTransitionValidation transition,
        DateTimeOffset timestamp)
    {
        ArgumentNullException.ThrowIfNull(transition);

        return Create(
            context,
            timestamp,
            AuditCommitPhase.PreCommit,
            decision: "reject",
            reasonCode: transition.ReasonCode,
            stateTransition: transition.Transition.ToString(),
            outcome: "rejected");
    }

    private static AuditEnvelope Create(
        ChatBotGatewayContext context,
        DateTimeOffset timestamp,
        AuditCommitPhase phase,
        string decision,
        string reasonCode,
        string stateTransition,
        string outcome,
        string? resourceId = null)
    {
        ArgumentNullException.ThrowIfNull(context);

        string commandName = CommandName(context);

        // The post-commit envelope references the durable aggregate identity (the dispatched NoteId) when the
        // dispatcher resolved one; pre-commit / rejection envelopes have no aggregate yet and fall back to the
        // command id. The value is still a safe, metadata-only ULID token (no payload).
        string auditedResourceId = AuditMetadata.IsSafeStableIdentifier(resourceId)
            ? resourceId!
            : context.Submission.Request.CommandId;

        return new AuditEnvelope(
            context.TenantBinding.TenantId,
            context.Actor.ActorId,
            ActorType(context),
            commandName,
            auditedResourceId,
            decision,
            reasonCode,
            context.Submission.CorrelationId,
            timestamp,
            NoPayloadPolicySnapshotId,
            SourceEvidenceRefs(context, phase),
            AuditMetadata.SafeOptionalToken(IdempotencyKey(context)),
            stateTransition,
            CoarseUserFacingRedactionStage.MetadataOnlyDecision,
            outcome,
            phase,
            EnvelopeSchemaVersion,
            PredecessorHash: null,
            ChatBotSurfaceOrigins.ToWireValue(context.Submission.Origin));
    }

    private static string CommandName(ChatBotGatewayContext context)
    {
        string? runtimeTypeName = context.Submission.Request.Command?.GetType().Name;
        if (!string.IsNullOrWhiteSpace(runtimeTypeName) &&
            !string.Equals(runtimeTypeName, "JsonElement", StringComparison.Ordinal))
        {
            return AuditMetadata.SafeCommandName(runtimeTypeName);
        }

        return AuditMetadata.SafeCommandName(context.Submission.Request.CommandType);
    }

    private static string ActorType(ChatBotGatewayContext context)
    {
        string? actorType = context.Actor.Principal.Claims
            .FirstOrDefault(static claim => string.Equals(claim.Type, "actor_type", StringComparison.Ordinal))?
            .Value;

        return AuditMetadata.SafeActorType(actorType);
    }

    private static IReadOnlyList<string> SourceEvidenceRefs(ChatBotGatewayContext context, AuditCommitPhase phase)
    {
        List<string> refs =
        [
            $"command:{context.Submission.Request.CommandId}",
            $"correlation:{context.Submission.CorrelationId}",
            $"phase:{PhaseName(phase)}",
        ];

        refs.AddRange(AssociationDecisionEvidenceRefs(context));
        refs.AddRange(AssociationCorrectionEvidenceRefs(context));
        return refs;
    }

    private static string PhaseName(AuditCommitPhase phase)
        => phase switch
        {
            AuditCommitPhase.PreCommit => "pre-commit",
            AuditCommitPhase.PostCommit => "post-commit",
            _ => throw new ArgumentOutOfRangeException(nameof(phase), phase, "Unsupported audit phase."),
        };

    private static string? IdempotencyKey(ChatBotGatewayContext context)
        => context.Idempotency?.CoarseKeyHash;

    private static IEnumerable<string> AssociationDecisionEvidenceRefs(ChatBotGatewayContext context)
    {
        string commandType = context.Submission.Request.CommandType ?? string.Empty;
        if (commandType is not (nameof(AssociateEmailToProject)
            or nameof(RejectEmailProjectAssociation)
            or nameof(DeferEmailProjectAssociation)
            or nameof(MarkEmailAssociationNeedsReview)))
        {
            yield break;
        }

        JsonElement element = context.Submission.Request.Command is JsonElement json
            ? json
            : JsonSerializer.SerializeToElement(context.Submission.Request.Command, new JsonSerializerOptions(JsonSerializerDefaults.Web));

        if (TryReadString(element, "decisionKind", out string? decisionKind))
        {
            yield return $"decision-kind:{AuditMetadata.SafeOptionalToken(decisionKind)}";
        }

        if (TryReadString(element, "candidateEvidenceFingerprint", out string? fingerprint))
        {
            yield return $"evidence-fingerprint:{AuditMetadata.SafeOptionalToken(fingerprint)}";
        }

        if (TryReadString(element, "associationId", out string? associationId))
        {
            yield return $"association:{AuditMetadata.SafeOptionalToken(associationId)}";
        }
    }

    private static IEnumerable<string> AssociationCorrectionEvidenceRefs(ChatBotGatewayContext context)
    {
        string commandType = context.Submission.Request.CommandType ?? string.Empty;
        if (!string.Equals(commandType, nameof(CorrectEmailProjectAssociation), StringComparison.Ordinal))
        {
            yield break;
        }

        JsonElement element = context.Submission.Request.Command is JsonElement json
            ? json
            : JsonSerializer.SerializeToElement(context.Submission.Request.Command, new JsonSerializerOptions(JsonSerializerDefaults.Web));

        if (TryReadString(element, "correctionKind", out string? correctionKind))
        {
            yield return $"correction-kind:{AuditMetadata.SafeOptionalToken(correctionKind)}";
        }

        if (TryReadString(element, "candidateEvidenceFingerprint", out string? fingerprint))
        {
            yield return $"evidence-fingerprint:{AuditMetadata.SafeOptionalToken(fingerprint)}";
        }

        if (TryReadString(element, "associationId", out string? associationId))
        {
            yield return $"association:{AuditMetadata.SafeOptionalToken(associationId)}";
        }

        if (TryReadString(element, "predecessorAssociationId", out string? predecessorAssociationId))
        {
            yield return $"predecessor-association:{AuditMetadata.SafeOptionalToken(predecessorAssociationId)}";
        }
    }

    private static bool TryReadString(JsonElement element, string propertyName, out string? value)
    {
        value = null;
        if (element.ValueKind != JsonValueKind.Object ||
            !element.TryGetProperty(propertyName, out JsonElement property) ||
            property.ValueKind != JsonValueKind.String)
        {
            return false;
        }

        value = property.GetString();
        return !string.IsNullOrWhiteSpace(value);
    }
}
