using Hexalith.ChatBot.Server.Authentication;

namespace Hexalith.ChatBot.Server.Gateway.Stages;

/// <summary>Binds an immutable authenticated subject and actor class.</summary>
internal sealed class ClaimsAuthenticationStage : IAuthenticationStage
{
    /// <inheritdoc/>
    public ValueTask<ChatBotAuthenticationResult> AuthenticateAsync(ChatBotCommandSubmission submission, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(submission);
        cancellationToken.ThrowIfCancellationRequested();
        return ValueTask.FromResult(ChatBotRequestContextResolver.TryResolve(submission.Principal, submission.Origin, out ChatBotRequestContext? context, out string reason)
            ? new ChatBotAuthenticationResult(new ChatBotAuthenticatedActor(context!.SubjectId, context.Principal, context.ActorClass, context.ServiceClientId, context), string.Empty)
            : ChatBotAuthenticationResult.Denied(reason));
    }
}
