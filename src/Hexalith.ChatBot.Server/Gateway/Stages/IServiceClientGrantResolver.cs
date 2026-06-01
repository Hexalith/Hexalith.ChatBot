using Hexalith.ChatBot.Server.Gateway;

namespace Hexalith.ChatBot.Server.Gateway.Stages;

internal interface IServiceClientGrantResolver
{
    ValueTask<ServiceClientGrantResolution> ResolveAsync(
        ChatBotCommandSubmission submission,
        ChatBotAuthenticatedActor actor,
        ChatBotTenantBinding tenantBinding,
        CancellationToken cancellationToken);
}
