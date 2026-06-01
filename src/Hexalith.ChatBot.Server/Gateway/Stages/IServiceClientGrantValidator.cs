using Hexalith.ChatBot.Server.Gateway;

namespace Hexalith.ChatBot.Server.Gateway.Stages;

internal interface IServiceClientGrantValidator
{
    ValueTask<ChatBotAuthorizationResult> ValidateAsync(
        ChatBotCommandSubmission submission,
        ChatBotAuthenticatedActor actor,
        ChatBotTenantBinding tenantBinding,
        CancellationToken cancellationToken);
}
