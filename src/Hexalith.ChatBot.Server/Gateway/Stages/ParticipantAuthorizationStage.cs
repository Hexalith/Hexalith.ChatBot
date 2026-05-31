using System.Security.Claims;

using Hexalith.ChatBot.Server.Gateway;

namespace Hexalith.ChatBot.Server.Gateway.Stages;

internal sealed class ParticipantAuthorizationStage : IAuthorizationStage
{
    public const string ParticipantAuthorityClaim = "chatbot:participant-authority";
    public const string UnresolvedValue = "unresolved";
    public const string EmailOnlyValue = "email-only";
    public const string UnauthorizedValue = "unauthorized";
    public const string DirectoryDegradedValue = "directory-degraded";

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

        return ValueTask.FromResult(ChatBotAuthorizationResult.Allowed());
    }
}
