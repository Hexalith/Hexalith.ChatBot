using System.Text.Json;

using Hexalith.ChatBot.Client.Generated;
using Hexalith.ChatBot.Contracts.Identities;
using Hexalith.ChatBot.Server.Authentication;
using Hexalith.ChatBot.Server.Audit;
using Hexalith.ChatBot.Server.Gateway.Idempotency;
using Hexalith.ChatBot.Server.Gateway.Stages;
using Hexalith.EventStore.Contracts.Commands;
using Hexalith.EventStore.Contracts.Events;
using Hexalith.EventStore.DomainService;

namespace Hexalith.ChatBot.Server.Gateway;

internal sealed class ChatBotDomainServiceAdmissionStage(
    ChatBotCommandAdmissionPipeline admission,
    IIdempotencyStore idempotencyStore,
    IChatBotAdmissionMarker admissionMarker,
    ChatBotRequestContextResolver requestContextResolver) : IDomainServiceAdmissionStage
{
    public string Name => "chatbot-command-gateway";

    public async Task<DomainServiceAdmissionResult> EvaluateAsync(
        DomainServiceAdmissionContext context,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);

        if (admissionMarker.IsValid(context.Command))
        {
            return DomainServiceAdmissionResult.Accepted();
        }

        ChatBotRequestContext? requestContext = requestContextResolver.ResolveCurrent();
        if (requestContext is null || !string.Equals(context.Command.UserId, requestContext.SubjectId, StringComparison.Ordinal))
        {
            return await RejectTransportAsync(context.Command, requestContext, ChatBotAuthorizationReasonCodes.AuthenticationDenied, cancellationToken).ConfigureAwait(false);
        }

        if (!string.Equals(context.Command.TenantId, requestContext.TenantId, StringComparison.Ordinal))
        {
            return await RejectTransportAsync(context.Command, requestContext, ChatBotAuthorizationReasonCodes.TenantMismatch, cancellationToken).ConfigureAwait(false);
        }

        if (!TryCreateSubmission(context.Command, requestContext, out ChatBotCommandSubmission? submission, out string reasonCode))
        {
            return Rejected(context.Command, reasonCode);
        }

        ChatBotCommandAdmissionDecision decision = await admission
            .AdmitAsync(submission!, cancellationToken)
            .ConfigureAwait(false);

        if (decision.IsAccepted)
        {
            if (decision.Idempotency is not null)
            {
                await idempotencyStore
                    .AbortAdmissionAsync(decision.Idempotency, cancellationToken)
                    .ConfigureAwait(false);
            }

            return DomainServiceAdmissionResult.Accepted();
        }

        // Duplicate-replay posture divergence (intended, permitted by AC3's "typed no-op/rejection posture"): the
        // HTTP gateway returns AcceptedResult(priorOutcome) (idempotent success), while this SDK /process path
        // surfaces a typed rejection (IsRejection == true) carrying DuplicateReplayPriorOutcome. The duplicate's
        // side effects (operation-status upsert, suppressed-intake audit) are still recorded once inside the shared
        // pipeline; only the wire shape differs. In the live topology /process is only reached via the marker
        // short-circuit above, so callers observe the gateway's idempotent-success surface.
        if (decision.Kind == ChatBotCommandAdmissionDecisionKind.ReplayPriorOutcome)
        {
            return Rejected(context.Command, ChatBotAuthorizationReasonCodes.DuplicateReplayPriorOutcome);
        }

        return Rejected(context.Command, decision.ReasonCode ?? ChatBotAuthorizationReasonCodes.AuthorizationDenied);
    }

    private async Task<DomainServiceAdmissionResult> RejectTransportAsync(CommandEnvelope command, ChatBotRequestContext? context, string reasonCode, CancellationToken cancellationToken)
    {
        await admission.RejectTransportAsync(command, context, reasonCode, cancellationToken).ConfigureAwait(false);
        return Rejected(command, reasonCode);
    }

    private static bool TryCreateSubmission(
        CommandEnvelope command,
        ChatBotRequestContext requestContext,
        out ChatBotCommandSubmission? submission,
        out string reasonCode)
    {
        submission = null;
        reasonCode = string.Empty;

        if (!TryReadPayload(command.Payload, out JsonElement payload))
        {
            reasonCode = ChatBotAuthorizationReasonCodes.InvalidCommandPayload;
            return false;
        }

        string? taskId = SafeExtension(command, "taskId");
        string? replayRunId = SafeExtension(command, "replayRunId");
        submission = new ChatBotCommandSubmission(
            requestContext.Principal,
            new CommandSubmissionRequest
            {
                CommandId = command.MessageId,
                CommandType = command.CommandType,
                Command = payload,
                RequestSchemaVersion = CommandSubmissionRequestRequestSchemaVersion.V1,
            },
            command.CorrelationId,
            ChatBotTaskId.TryParse(taskId, out ChatBotTaskId parsedTaskId) ? parsedTaskId.Value : null,
            requestContext.Origin,
            replayRunId);
        return true;
    }

    private static bool TryReadPayload(byte[] payload, out JsonElement element)
    {
        element = default;
        try
        {
            using JsonDocument document = JsonDocument.Parse(payload);
            element = document.RootElement.Clone();
            return true;
        }
        catch (JsonException)
        {
            return false;
        }
    }

    private static string? SafeExtension(CommandEnvelope command, string key)
    {
        if (command.Extensions is null ||
            !command.Extensions.TryGetValue(key, out string? value))
        {
            return null;
        }

        return AuditMetadata.SafeOptionalToken(value);
    }

    private static DomainServiceAdmissionResult Rejected(CommandEnvelope command, string reasonCode)
        => DomainServiceAdmissionResult.Rejected(
            [
                new ChatBotDomainServiceAdmissionRejected(
                    AuditMetadata.SafeOptionalToken(command.MessageId),
                    AuditMetadata.SafeCommandName(command.CommandType),
                    AuditMetadata.SafeOptionalToken(reasonCode) ?? ChatBotAuthorizationReasonCodes.AuthorizationDenied,
                    AuditMetadata.SafeOptionalToken(command.CorrelationId)),
            ]);
}
