using Hexalith.ChatBot.Contracts.Commands;
using Hexalith.ChatBot.Contracts.Enums;
using Hexalith.ChatBot.Contracts.Identities;
using Hexalith.ChatBot.Server.Audit;
using Hexalith.ChatBot.Server.Gateway;
using Hexalith.ChatBot.Server.Notifications;

namespace Hexalith.ChatBot.Server.Gateway.Stages;

internal sealed class ServiceClientGrantValidator(
    IServiceClientGrantResolver resolver,
    ISystemClock clock,
    ISpineCommandAllowlist spineCommandAllowlist,
    IServiceClientControlStateProvider? controlStateProvider = null,
    IServiceClientRateLimitProvider? rateLimitProvider = null,
    IServiceClientCommandHistory? commandHistory = null,
    IAiActorControlStateProvider? aiActorControlStateProvider = null) : IServiceClientGrantValidator
{
    private readonly IServiceClientControlStateProvider _controlStateProvider =
        controlStateProvider ?? new AlwaysActiveServiceClientControlStateProvider();

    private readonly IAiActorControlStateProvider _aiActorControlStateProvider =
        aiActorControlStateProvider ?? new AlwaysActiveAiActorControlStateProvider();

    private readonly IServiceClientRateLimitProvider _rateLimitProvider =
        rateLimitProvider ?? new AlwaysUnlimitedServiceClientRateLimitProvider();

    private readonly IServiceClientCommandHistory _commandHistory =
        commandHistory ?? new EmptyServiceClientCommandHistory();

    public async ValueTask<ChatBotAuthorizationResult> ValidateAsync(
        ChatBotCommandSubmission submission,
        ChatBotAuthenticatedActor actor,
        ChatBotTenantBinding tenantBinding,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(submission);
        ArgumentNullException.ThrowIfNull(actor);
        ArgumentNullException.ThrowIfNull(tenantBinding);
        cancellationToken.ThrowIfCancellationRequested();

        if (!RequiresGrant(actor))
        {
            return ChatBotAuthorizationResult.Allowed();
        }

        ServiceClientGrantResolution resolution = await resolver
            .ResolveAsync(submission, actor, tenantBinding, cancellationToken)
            .ConfigureAwait(false);
        if (!resolution.IsResolved)
        {
            return ChatBotAuthorizationResult.Denied(resolution.ReasonCode);
        }

        ServiceClientGrant grant = resolution.Grant!;
        if (string.IsNullOrWhiteSpace(actor.ServiceClientId) ||
            !string.Equals(grant.ServiceClientId, actor.ServiceClientId, StringComparison.Ordinal))
        {
            return ChatBotAuthorizationResult.Denied(ChatBotAuthorizationReasonCodes.ServiceClientGrantMissing);
        }

        if (!string.Equals(grant.TenantId, tenantBinding.TenantId, StringComparison.Ordinal))
        {
            return ChatBotAuthorizationResult.Denied(ChatBotAuthorizationReasonCodes.ServiceClientGrantTenantMismatch);
        }

        if (grant.SurfaceOrigin != submission.Origin)
        {
            return ChatBotAuthorizationResult.Denied(ChatBotAuthorizationReasonCodes.ServiceClientWrongSurface);
        }

        if (grant.ExpiresAt <= clock.UtcNow)
        {
            return ChatBotAuthorizationResult.Denied(ChatBotAuthorizationReasonCodes.ServiceClientGrantExpired);
        }

        if (grant.IsRevoked)
        {
            return ChatBotAuthorizationResult.Denied(ChatBotAuthorizationReasonCodes.ServiceClientGrantRevoked);
        }

        // FR74 two-person AI-actor governance control: a disabled AI actor fails closed before any grant
        // scope/allowlist check or downstream AI approval gate. This is a dedicated AI-actor control plane gated on
        // the `ai` actor type — placed before the service-client control-state block so an AI actor gets the precise
        // `ai_actor_disabled` reason rather than falling through to `service_client_disabled`. Distinct from the Epic 5
        // grant-lifecycle revocation above; each AI actor's control state is independent (isolation). The control
        // state is read from a metadata-only seam — no credential/OAuth fingerprint or model prompt is read or exposed.
        if (string.Equals(actor.ActorType, ParticipantAuthorizationStage.AiActorValue, StringComparison.Ordinal))
        {
            AiActorControlState aiActorControlState = await _aiActorControlStateProvider
                .GetControlStateAsync(grant.TenantId, grant.ServiceClientId, cancellationToken)
                .ConfigureAwait(false);
            if (aiActorControlState == AiActorControlState.Disabled)
            {
                return ChatBotAuthorizationResult.Denied(ChatBotAuthorizationReasonCodes.AiActorDisabled);
            }

            // FR74 AI-actor quarantine (contained for review): a quarantined AI actor's new proposals/commands fail
            // closed here, beside the disabled check, with the distinct `ai_actor_quarantined` reason — before any
            // grant scope/allowlist check and before the proposal reaches the downstream AiActionApprovalGate.
            if (aiActorControlState == AiActorControlState.Quarantined)
            {
                return ChatBotAuthorizationResult.Denied(ChatBotAuthorizationReasonCodes.AiActorQuarantined);
            }
        }

        // FR74 two-person governance control: a disabled service client fails closed before any grant
        // scope/allowlist check or durable work. Distinct from the Epic 5 grant-lifecycle revocation above;
        // each service client's control state is independent (isolation). The control state is read from a
        // metadata-only seam — no credential/OAuth fingerprint is read or exposed.
        ServiceClientControlState controlState = await _controlStateProvider
            .GetControlStateAsync(grant.TenantId, grant.ServiceClientId, cancellationToken)
            .ConfigureAwait(false);
        if (controlState == ServiceClientControlState.Disabled)
        {
            return ChatBotAuthorizationResult.Denied(ChatBotAuthorizationReasonCodes.ServiceClientDisabled);
        }

        if (controlState == ServiceClientControlState.Quarantined)
        {
            return ChatBotAuthorizationResult.Denied(ChatBotAuthorizationReasonCodes.ServiceClientQuarantined);
        }

        if (IsOverScoped(grant))
        {
            return ChatBotAuthorizationResult.Denied(ChatBotAuthorizationReasonCodes.ServiceClientGrantOverScoped);
        }

        string commandName = AuditMetadata.SafeCommandName(submission.Request.CommandType);
        if (!grant.AllowedCommandNames.Contains(commandName, StringComparer.Ordinal))
        {
            return ChatBotAuthorizationResult.Denied(ChatBotAuthorizationReasonCodes.ServiceClientGrantUnderScoped);
        }

        // Story 7.17: rate-limit is the FINAL admission gate — placed after every security check (control state,
        // grant lifecycle, scope/allowlist) so that only otherwise-fully-admissible commands count against the budget,
        // and a disabled/quarantined/expired/revoked/over-/under-scoped command keeps its precise reason code
        // (rate-limit never masks a security denial). The budget + recent admitted-command history are read from
        // metadata-only seams — no credential/OAuth fingerprint is read or exposed. Each client's budget/counter is
        // independent (NFR30 isolation). The trailing-window count is server-measured UTC age against the injected clock.
        ServiceClientRateLimitState? rateLimit = await _rateLimitProvider
            .GetRateLimitAsync(grant.TenantId, grant.ServiceClientId, cancellationToken)
            .ConfigureAwait(false);
        if (rateLimit is not null)
        {
            IReadOnlyList<DateTimeOffset> recentAdmitted = await _commandHistory
                .GetRecentAdmittedAsync(grant.TenantId, grant.ServiceClientId, cancellationToken)
                .ConfigureAwait(false);
            int windowCount = NotificationThrottleEvaluator.CountInTrailingWindow(
                recentAdmitted, clock.UtcNow, rateLimit.WindowDuration);
            if (windowCount >= rateLimit.EffectiveBudget)
            {
                return ChatBotAuthorizationResult.Denied(ChatBotAuthorizationReasonCodes.ServiceClientRateLimited);
            }
        }

        return ChatBotAuthorizationResult.Allowed(new ServiceClientGrantEvidence(
            grant.ServiceClientId,
            grant.ClientClass,
            grant.TenantId,
            grant.GrantId,
            grant.Scopes,
            grant.ExpiresAt,
            grant.SurfaceOrigin,
            grant.CommandSetVersion,
            grant.DelegatedUserId,
            grant.OAuthGrantEvidenceFingerprint));
    }

    private static bool RequiresGrant(ChatBotAuthenticatedActor actor)
        => string.Equals(actor.ActorType, ParticipantAuthorizationStage.ServiceActorValue, StringComparison.Ordinal) ||
            string.Equals(actor.ActorType, ParticipantAuthorizationStage.AiActorValue, StringComparison.Ordinal);

    private bool IsOverScoped(ServiceClientGrant grant)
        => grant.AllowedCommandNames.Any(commandName =>
            string.Equals(commandName, "*", StringComparison.Ordinal) ||
            !spineCommandAllowlist.IsAllowed(commandName) ||
            string.Equals(commandName, nameof(SetAssociationConfidenceThresholds), StringComparison.Ordinal));
}
