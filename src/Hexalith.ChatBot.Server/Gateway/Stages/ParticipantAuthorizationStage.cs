using System.Security.Claims;

using Hexalith.ChatBot.Contracts.Commands;
using Hexalith.ChatBot.Server.Gateway;

namespace Hexalith.ChatBot.Server.Gateway.Stages;

internal sealed class ParticipantAuthorizationStage : IAuthorizationStage
{
    public const string ParticipantAuthorityClaim = "chatbot:participant-authority";
    public const string UnresolvedValue = "unresolved";
    public const string EmailOnlyValue = "email-only";
    public const string UnauthorizedValue = "unauthorized";
    public const string DirectoryDegradedValue = "directory-degraded";
    public const string ActorTypeClaim = "chatbot:actor-type";
    public const string TenantRoleClaim = "chatbot:tenant-role";
    public const string HumanActorValue = "human";
    public const string TenantAdminValue = "tenant-admin";

    public ValueTask<ChatBotAuthorizationResult> AuthorizeAsync(
        ChatBotCommandSubmission submission,
        ChatBotAuthenticatedActor actor,
        ChatBotTenantBinding tenantBinding,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(submission);
        ArgumentNullException.ThrowIfNull(actor);
        ArgumentNullException.ThrowIfNull(tenantBinding);
        cancellationToken.ThrowIfCancellationRequested();

        string[] authorities = actor.Principal
            .FindAll(ParticipantAuthorityClaim)
            .Select(static claim => claim.Value)
            .Where(static value => !string.IsNullOrWhiteSpace(value))
            .ToArray();

        if (authorities.Contains(DirectoryDegradedValue, StringComparer.Ordinal))
        {
            return ValueTask.FromResult(ChatBotAuthorizationResult.Denied(ChatBotAuthorizationReasonCodes.ParticipantDirectoryDegraded));
        }

        if (authorities.Contains(UnresolvedValue, StringComparer.Ordinal))
        {
            return ValueTask.FromResult(ChatBotAuthorizationResult.Denied(ChatBotAuthorizationReasonCodes.UnresolvedParticipant));
        }

        if (authorities.Contains(EmailOnlyValue, StringComparer.Ordinal) ||
            authorities.Contains(UnauthorizedValue, StringComparer.Ordinal))
        {
            return ValueTask.FromResult(ChatBotAuthorizationResult.Denied(ChatBotAuthorizationReasonCodes.UnauthorizedParticipant));
        }

        if (string.Equals(submission.Request.CommandType, nameof(SetAssociationConfidenceThresholds), StringComparison.Ordinal) &&
            !IsTenantAdminHuman(actor.Principal))
        {
            return ValueTask.FromResult(ChatBotAuthorizationResult.Denied(ChatBotAuthorizationReasonCodes.ThresholdPolicyUnauthorized));
        }

        return ValueTask.FromResult(ChatBotAuthorizationResult.Allowed());
    }

    private static bool IsTenantAdminHuman(ClaimsPrincipal principal)
        => principal.HasClaim(ActorTypeClaim, HumanActorValue) &&
            principal.HasClaim(TenantRoleClaim, TenantAdminValue);
}
