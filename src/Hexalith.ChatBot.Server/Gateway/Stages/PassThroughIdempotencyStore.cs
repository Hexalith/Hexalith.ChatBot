using Hexalith.ChatBot.Server.Gateway;

namespace Hexalith.ChatBot.Server.Gateway.Stages;

internal sealed class PassThroughIdempotencyStore : IIdempotencyStore
{
    public ValueTask RecordAdmissionAsync(ChatBotGatewayContext context, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        return ValueTask.CompletedTask;
    }
}
