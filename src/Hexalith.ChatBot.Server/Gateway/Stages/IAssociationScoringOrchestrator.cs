using Hexalith.ChatBot.Contracts.Commands;

namespace Hexalith.ChatBot.Server.Gateway.Stages;

internal interface IAssociationScoringOrchestrator
{
    ValueTask<ScoreMailboxMessageAssociation> ScoreAsync(
        ScoreMailboxMessageAssociation command,
        ChatBotGatewayContext context,
        CancellationToken cancellationToken);
}
