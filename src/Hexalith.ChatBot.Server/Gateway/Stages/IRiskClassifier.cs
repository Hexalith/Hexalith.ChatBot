using Hexalith.ChatBot.Server.Gateway;

namespace Hexalith.ChatBot.Server.Gateway.Stages;

internal interface IRiskClassifier
{
    ValueTask<ChatBotRiskClassification> ClassifyAsync(ChatBotGatewayContext context, CancellationToken cancellationToken);
}
