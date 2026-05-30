using Hexalith.ChatBot.Server.Gateway;

namespace Hexalith.ChatBot.Server.Gateway.Stages;

internal interface IAuditWriter
{
    ValueTask RecordAuthorizationFailureAsync(ChatBotAuthorizationFailureAuditFact fact, CancellationToken cancellationToken);

    ValueTask RecordPreCommitAsync(ChatBotGatewayContext context, CancellationToken cancellationToken);

    ValueTask RecordPostCommitAsync(ChatBotGatewayContext context, CancellationToken cancellationToken);
}
