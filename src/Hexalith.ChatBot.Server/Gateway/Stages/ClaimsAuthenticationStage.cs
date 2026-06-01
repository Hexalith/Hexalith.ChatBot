using System.Security.Claims;

using Hexalith.ChatBot.Server.Audit;
using Hexalith.ChatBot.Server.Gateway;

namespace Hexalith.ChatBot.Server.Gateway.Stages;

internal sealed class ClaimsAuthenticationStage : IAuthenticationStage
{
    private static readonly string[] ActorTypeClaimTypes = [ParticipantAuthorizationStage.ActorTypeClaim, "actor_type"];
    private static readonly string[] ServiceClientIdClaimTypes = [ClaimsServiceClientGrantResolver.ServiceClientIdClaim, "azp", "client_id"];

    public ValueTask<ChatBotAuthenticationResult> AuthenticateAsync(ChatBotCommandSubmission submission, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(submission);

        ClaimsIdentity? identity = submission.Principal.Identity as ClaimsIdentity;
        if (identity is null || !identity.IsAuthenticated)
        {
            return ValueTask.FromResult(ChatBotAuthenticationResult.Denied(ChatBotAuthorizationReasonCodes.AuthenticationDenied));
        }

        string? actorId = submission.Principal.FindFirstValue("sub");
        if (!AuditMetadata.IsSafeStableIdentifier(actorId))
        {
            return ValueTask.FromResult(ChatBotAuthenticationResult.Denied(ChatBotAuthorizationReasonCodes.AuthenticationDenied));
        }

        string? serviceClientId = ResolveServiceClientId(submission.Principal);
        string actorType = ResolveActorType(submission.Principal, serviceClientId);
        return ValueTask.FromResult(ChatBotAuthenticationResult.Authenticated(actorId!, submission.Principal, actorType, serviceClientId));
    }

    private static string ResolveActorType(ClaimsPrincipal principal, string? serviceClientId)
    {
        string? claimValue = ActorTypeClaimTypes
            .Select(type => principal.FindFirstValue(type))
            .FirstOrDefault(static value => !string.IsNullOrWhiteSpace(value));

        if (!string.IsNullOrWhiteSpace(serviceClientId))
        {
            return string.Equals(claimValue, ParticipantAuthorizationStage.AiActorValue, StringComparison.Ordinal)
                ? ParticipantAuthorizationStage.AiActorValue
                : ParticipantAuthorizationStage.ServiceActorValue;
        }

        if (AuditMetadata.SafeActorType(claimValue) is string safe && string.Equals(safe, claimValue, StringComparison.Ordinal))
        {
            return safe;
        }

        return AuditMetadata.DefaultActorType;
    }

    private static string? ResolveServiceClientId(ClaimsPrincipal principal)
    {
        string? direct = ServiceClientIdClaimTypes
            .Select(type => principal.FindFirstValue(type))
            .FirstOrDefault(AuditMetadata.IsSafeStableIdentifier);
        if (!string.IsNullOrWhiteSpace(direct))
        {
            return direct;
        }

        string? preferredUserName = principal.FindFirstValue("preferred_username");
        const string keycloakServicePrefix = "service-account-";
        if (preferredUserName?.StartsWith(keycloakServicePrefix, StringComparison.Ordinal) == true)
        {
            string serviceClientId = preferredUserName[keycloakServicePrefix.Length..];
            return AuditMetadata.IsSafeStableIdentifier(serviceClientId) ? serviceClientId : null;
        }

        return null;
    }
}
