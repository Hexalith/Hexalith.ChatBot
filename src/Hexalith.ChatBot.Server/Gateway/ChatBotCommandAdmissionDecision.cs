using Hexalith.ChatBot.Client.Generated;
using Hexalith.ChatBot.Contracts.Enums;
using Hexalith.ChatBot.Server.Audit;
using Hexalith.ChatBot.Server.Gateway.Idempotency;
using Hexalith.ChatBot.Server.Gateway.Status;
using Hexalith.ChatBot.Server.Gateway.Stages;
using Hexalith.ChatBot.Server.Lifecycle.StateModel;
using Hexalith.ChatBot.Server.Observability;

namespace Hexalith.ChatBot.Server.Gateway;

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
