using Hexalith.ChatBot.Server.Gateway;

namespace Hexalith.ChatBot.Server.Gateway.Stages;

internal interface ITenantBindingStage
{
    ValueTask<ChatBotTenantBindingResult> BindTenantAsync(
        ChatBotCommandSubmission submission,
        ChatBotAuthenticatedActor actor,
        CancellationToken cancellationToken);
}
