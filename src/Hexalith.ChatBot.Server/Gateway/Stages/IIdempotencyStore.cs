using Hexalith.ChatBot.Server.Gateway;

namespace Hexalith.ChatBot.Server.Gateway.Stages;

internal interface IIdempotencyStore
{
    ValueTask RecordAdmissionAsync(ChatBotGatewayContext context, CancellationToken cancellationToken);
}
