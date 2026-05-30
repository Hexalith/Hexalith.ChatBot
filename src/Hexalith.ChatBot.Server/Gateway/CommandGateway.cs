using Hexalith.ChatBot.Client.Generated;

using Hexalith.ChatBot.Server.Gateway.Stages;

namespace Hexalith.ChatBot.Server.Gateway;

internal sealed class CommandGateway(
    IAuthenticationStage authentication,
    ITenantBindingStage tenantBinding,
    IAuthorizationStage authorization,
    IRiskClassifier riskClassifier,
    IApprovalGate approvalGate,
    IIdempotencyStore idempotencyStore,
    IAuditWriter auditWriter,
    ICommandDispatcher dispatcher)
{
    public async ValueTask<ChatBotGatewayResult> SubmitAsync(ChatBotCommandSubmission submission, CancellationToken cancellationToken)
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
            return await DenyAsync(
                submission,
                bindingResult.Binding?.TenantId ?? "unavailable",
                actor.ActorId,
                bindingResult.ReasonCode,
                cancellationToken)
                .ConfigureAwait(false);
        }

        ChatBotTenantBinding binding = bindingResult.Binding!;
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

        ChatBotGatewayContext context = new(submission, actor, binding);
        _ = await riskClassifier.ClassifyAsync(context, cancellationToken).ConfigureAwait(false);
        _ = await approvalGate.EvaluateAsync(context, cancellationToken).ConfigureAwait(false);
        await idempotencyStore.RecordAdmissionAsync(context, cancellationToken).ConfigureAwait(false);
        await auditWriter.RecordPreCommitAsync(context, cancellationToken).ConfigureAwait(false);
        ChatBotDispatchResult dispatchResult = await dispatcher.DispatchAsync(context, cancellationToken).ConfigureAwait(false);
        await auditWriter.RecordPostCommitAsync(context, cancellationToken).ConfigureAwait(false);

        return ChatBotGatewayResult.AcceptedResult(new CommandSubmissionResponse
        {
            CommandId = submission.Request.CommandId,
            CorrelationId = submission.CorrelationId,
            TaskId = submission.TaskId,
            LifecycleState = LifecycleState.Accepted,
            AcceptedAt = dispatchResult.AcceptedAt,
        });
    }

    private async ValueTask<ChatBotGatewayResult> DenyAsync(
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
                    submission.Request.CommandType,
                    reasonCode,
                    submission.CorrelationId,
                    submission.TaskId),
                cancellationToken)
            .ConfigureAwait(false);

        return ChatBotGatewayResult.Denied(ChatBotProblemDetailsFactory.Create(reasonCode, submission.CorrelationId, submission.TaskId));
    }
}
