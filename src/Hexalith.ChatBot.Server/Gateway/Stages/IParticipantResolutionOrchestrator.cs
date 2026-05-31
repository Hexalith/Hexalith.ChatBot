using Hexalith.ChatBot.Contracts.Commands;

namespace Hexalith.ChatBot.Server.Gateway.Stages;

internal interface IParticipantResolutionOrchestrator
{
    ValueTask<ResolveMailboxMessageParticipants> ResolveAsync(
        ResolveMailboxMessageParticipants command,
        ChatBotGatewayContext context,
        CancellationToken cancellationToken);
}
