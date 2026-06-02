using Hexalith.ChatBot.Contracts.Commands;
using Hexalith.ChatBot.Contracts.Enums;
using Hexalith.ChatBot.Contracts.Identities;
using Hexalith.ChatBot.Server.Audit;
using Hexalith.ChatBot.Server.Gateway;

namespace Hexalith.ChatBot.Server.Gateway.Stages;

internal sealed class ServiceClientGrantValidator(
    IServiceClientGrantResolver resolver,
    ISystemClock clock,
    ISpineCommandAllowlist spineCommandAllowlist,
    IServiceClientControlStateProvider? controlStateProvider = null) : IServiceClientGrantValidator
{
    private readonly IServiceClientControlStateProvider _controlStateProvider =
        controlStateProvider ?? new AlwaysActiveServiceClientControlStateProvider();

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

        if (IsOverScoped(grant))
        {
            return ChatBotAuthorizationResult.Denied(ChatBotAuthorizationReasonCodes.ServiceClientGrantOverScoped);
        }

        string commandName = AuditMetadata.SafeCommandName(submission.Request.CommandType);
        if (!grant.AllowedCommandNames.Contains(commandName, StringComparer.Ordinal))
        {
            return ChatBotAuthorizationResult.Denied(ChatBotAuthorizationReasonCodes.ServiceClientGrantUnderScoped);
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
