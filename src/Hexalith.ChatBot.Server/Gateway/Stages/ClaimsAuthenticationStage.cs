using System.Security.Claims;

using Hexalith.ChatBot.Server.Gateway;

namespace Hexalith.ChatBot.Server.Gateway.Stages;

internal sealed class ClaimsAuthenticationStage : IAuthenticationStage
{
    public ValueTask<ChatBotAuthenticationResult> AuthenticateAsync(ChatBotCommandSubmission submission, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(submission);

        ClaimsIdentity? identity = submission.Principal.Identity as ClaimsIdentity;
        if (identity is null || !identity.IsAuthenticated)
        {
            return ValueTask.FromResult(ChatBotAuthenticationResult.Denied(ChatBotAuthorizationReasonCodes.AuthenticationDenied));
        }

        string? actorId = submission.Principal.FindFirstValue("sub");
        if (string.IsNullOrWhiteSpace(actorId))
        {
            return ValueTask.FromResult(ChatBotAuthenticationResult.Denied(ChatBotAuthorizationReasonCodes.AuthenticationDenied));
        }

        return ValueTask.FromResult(ChatBotAuthenticationResult.Authenticated(actorId, submission.Principal));
    }
}
