using Hexalith.ChatBot.Server.Gateway;

namespace Hexalith.ChatBot.Server.Audit;

internal static class AuditEnvelopeFactory
{
    private const string EnvelopeSchemaVersion = "chatbot.audit-envelope.v1";
    private const string MetadataOnlyRedactionDecision = "metadata_only";
    private const string NoPayloadPolicySnapshotId = "chatbot.gateway.policy-snapshot.v1";

    public static AuditEnvelope PreCommit(ChatBotGatewayContext context, DateTimeOffset timestamp)
        => Create(
            context,
            timestamp,
            AuditCommitPhase.PreCommit,
            decision: "allow",
            reasonCode: "pre_commit_gate",
            stateTransition: "admitted->dispatch_pending",
            outcome: "gate_passed");

    public static AuditEnvelope PostCommit(ChatBotGatewayContext context, ChatBotDispatchResult dispatchResult, DateTimeOffset timestamp)
    {
        ArgumentNullException.ThrowIfNull(dispatchResult);

        return Create(
            context,
            timestamp,
            AuditCommitPhase.PostCommit,
            decision: "allow",
            reasonCode: "eventstore_dispatch_accepted",
            stateTransition: "dispatch_pending->accepted",
            outcome: "accepted");
    }

    private static AuditEnvelope Create(
        ChatBotGatewayContext context,
        DateTimeOffset timestamp,
        AuditCommitPhase phase,
        string decision,
        string reasonCode,
        string stateTransition,
        string outcome)
    {
        ArgumentNullException.ThrowIfNull(context);

        string commandName = CommandName(context);

        return new AuditEnvelope(
            context.TenantBinding.TenantId,
            context.Actor.ActorId,
            ActorType(context),
            commandName,
            context.Submission.Request.CommandId,
            decision,
            reasonCode,
            context.Submission.CorrelationId,
            timestamp,
            NoPayloadPolicySnapshotId,
            SourceEvidenceRefs(context, phase),
            AuditMetadata.SafeOptionalToken(IdempotencyKey(context)),
            stateTransition,
            MetadataOnlyRedactionDecision,
            outcome,
            phase,
            EnvelopeSchemaVersion,
            PredecessorHash: null);
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
        =>
        [
            $"command:{context.Submission.Request.CommandId}",
            $"correlation:{context.Submission.CorrelationId}",
            $"phase:{PhaseName(phase)}",
        ];

    private static string PhaseName(AuditCommitPhase phase)
        => phase switch
        {
            AuditCommitPhase.PreCommit => "pre-commit",
            AuditCommitPhase.PostCommit => "post-commit",
            _ => throw new ArgumentOutOfRangeException(nameof(phase), phase, "Unsupported audit phase."),
        };

    private static string? IdempotencyKey(ChatBotGatewayContext context)
        => context.Actor.Principal.Claims
            .FirstOrDefault(static claim => string.Equals(claim.Type, "idempotency_key", StringComparison.Ordinal))?
            .Value;
}
