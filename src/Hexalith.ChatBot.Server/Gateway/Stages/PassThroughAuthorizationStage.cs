using Hexalith.ChatBot.Server.Gateway;

namespace Hexalith.ChatBot.Server.Gateway.Stages;

internal sealed class PassThroughAuthorizationStage : IAuthorizationStage
{
    public ValueTask<ChatBotAuthorizationResult> AuthorizeAsync(
        ChatBotCommandSubmission submission,
        ChatBotAuthenticatedActor actor,
        ChatBotTenantBinding tenantBinding,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(submission);
        ArgumentNullException.ThrowIfNull(actor);
        ArgumentNullException.ThrowIfNull(tenantBinding);
        return ValueTask.FromResult(ChatBotAuthorizationResult.Allowed());
    }
}
