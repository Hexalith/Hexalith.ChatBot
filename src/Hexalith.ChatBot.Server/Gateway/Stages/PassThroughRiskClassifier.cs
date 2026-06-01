using Hexalith.ChatBot.Server.Gateway;

namespace Hexalith.ChatBot.Server.Gateway.Stages;

internal sealed class PassThroughRiskClassifier : IRiskClassifier
{
    public ValueTask<ChatBotRiskClassification> ClassifyAsync(ChatBotGatewayContext context, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        context.SetRiskClassification(ChatBotRiskClassification.PassThrough);
        return ValueTask.FromResult(ChatBotRiskClassification.PassThrough);
    }
}
