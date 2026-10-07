using Hexalith.ChatBot.Server.Gateway;
using Hexalith.ChatBot.Server.Gateway.Stages;

namespace Hexalith.ChatBot.Server.Tests;

/// <summary>Advances authority state across an actual downstream asynchronous boundary.</summary>
internal sealed class TrustedAuthorityAdvancingRiskClassifier(Action advance) : IRiskClassifier
{
    /// <inheritdoc/>
    public async ValueTask<ChatBotRiskClassification> ClassifyAsync(ChatBotGatewayContext context, CancellationToken cancellationToken)
    {
        await Task.Yield();
        cancellationToken.ThrowIfCancellationRequested();
        advance();
        return ChatBotRiskClassification.PassThrough;
    }
}
