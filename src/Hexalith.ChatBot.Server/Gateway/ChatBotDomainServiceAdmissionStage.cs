using System.Security.Claims;
using System.Text.Json;

using Hexalith.ChatBot.Client.Generated;
using Hexalith.ChatBot.Contracts.Enums;
using Hexalith.ChatBot.Contracts.Identities;
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
    IChatBotAdmissionMarker admissionMarker) : IDomainServiceAdmissionStage
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

        if (!TryCreateSubmission(context.Command, out ChatBotCommandSubmission? submission, out string reasonCode))
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

    private static bool TryCreateSubmission(
        CommandEnvelope command,
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
        ChatBotSurfaceOrigin origin = ChatBotSurfaceOrigins.FromWireValueOrDefault(SafeExtension(command, "surfaceOrigin"));
        string? replayRunId = SafeExtension(command, "replayRunId");
        submission = new ChatBotCommandSubmission(
            PrincipalFromEnvelope(command),
            new CommandSubmissionRequest
            {
                CommandId = command.MessageId,
                CommandType = command.CommandType,
                Command = payload,
                RequestSchemaVersion = CommandSubmissionRequestRequestSchemaVersion.V1,
            },
            command.CorrelationId,
            ChatBotTaskId.TryParse(taskId, out ChatBotTaskId parsedTaskId) ? parsedTaskId.Value : null,
            origin,
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

    private static ClaimsPrincipal PrincipalFromEnvelope(CommandEnvelope command)
    {
        List<Claim> claims =
        [
            new("sub", command.UserId),
            new("eventstore:tenant", command.TenantId),
        ];

        AddClaimIfSafe(claims, command, ParticipantAuthorizationStage.ActorTypeClaim);
        AddClaimIfSafe(claims, command, ParticipantAuthorizationStage.TenantRoleClaim);
        AddClaimIfSafe(claims, command, ParticipantAuthorizationStage.ProjectOwnerClaim);
        AddRepeatedClaimsIfSafe(claims, command, ParticipantAuthorizationStage.ParticipantAuthorityClaim);

        AddClaimIfSafe(claims, command, ClaimsServiceClientGrantResolver.ServiceClientIdClaim);
        AddClaimIfSafe(claims, command, ClaimsServiceClientGrantResolver.ServiceClientClassClaim);
        AddClaimIfSafe(claims, command, ClaimsServiceClientGrantResolver.GrantIdClaim);
        AddClaimIfSafe(claims, command, ClaimsServiceClientGrantResolver.GrantTenantClaim);
        AddClaimIfSafe(claims, command, ClaimsServiceClientGrantResolver.GrantExpiryClaim);
        AddClaimIfSafe(claims, command, ClaimsServiceClientGrantResolver.GrantRevokedClaim);
        AddRepeatedClaimsIfSafe(claims, command, ClaimsServiceClientGrantResolver.GrantScopeClaim);
        AddRepeatedClaimsIfSafe(claims, command, ClaimsServiceClientGrantResolver.GrantCommandClaim);
        AddRepeatedClaimsIfSafe(claims, command, ClaimsServiceClientGrantResolver.GrantQueryClaim);
        AddClaimIfSafe(claims, command, ClaimsServiceClientGrantResolver.GrantSurfaceClaim);
        AddClaimIfSafe(claims, command, ClaimsServiceClientGrantResolver.DelegatedUserIdClaim);
        AddClaimIfSafe(claims, command, ClaimsServiceClientGrantResolver.OAuthGrantEvidenceFingerprintClaim);
        AddClaimIfSafe(claims, command, ClaimsServiceClientGrantResolver.CommandSetVersionClaim);

        return new ClaimsPrincipal(new ClaimsIdentity(claims, "eventstore-domain-service"));
    }

    private static void AddClaimIfSafe(List<Claim> claims, CommandEnvelope command, string claimType)
    {
        string? value = SafeExtension(command, claimType);
        if (!string.IsNullOrWhiteSpace(value))
        {
            claims.Add(new Claim(claimType, value));
        }
    }

    private static void AddRepeatedClaimsIfSafe(List<Claim> claims, CommandEnvelope command, string claimType)
    {
        string? value = SafeExtension(command, claimType);
        if (string.IsNullOrWhiteSpace(value))
        {
            return;
        }

        foreach (string item in value.Split('|', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            if (AuditMetadata.SafeOptionalToken(item) is { } safe)
            {
                claims.Add(new Claim(claimType, safe));
            }
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

/// <summary>
/// Metadata-only rejection emitted when ChatBot command admission denies an SDK <c>/process</c> request.
/// </summary>
/// <param name="CommandId">The safe command identifier when available.</param>
/// <param name="CommandType">The safe command type name.</param>
/// <param name="ReasonCode">The finite admission reason code.</param>
/// <param name="CorrelationId">The safe correlation identifier when available.</param>
public sealed record ChatBotDomainServiceAdmissionRejected(
    string? CommandId,
    string CommandType,
    string ReasonCode,
    string? CorrelationId) : IRejectionEvent;
