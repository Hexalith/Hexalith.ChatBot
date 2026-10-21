using Hexalith.ChatBot.Server.Gateway;
using Hexalith.ChatBot.Server.Gateway.Stages;

namespace Hexalith.ChatBot.Server.Tests.Gateway;

/// <summary>Synthetic malformed classifier outputs for admission containment tests.</summary>
internal sealed class SyntheticMissingRiskClassifier(int mode) : IRiskClassifier
{
    /// <inheritdoc />
    public ValueTask<ChatBotRiskClassification> ClassifyAsync(ChatBotGatewayContext context, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return mode switch
        {
            0 => ValueTask.FromResult<ChatBotRiskClassification>(null!),
            1 => ValueTask.FromResult(ChatBotRiskClassification.PassThrough),
            2 => throw new InvalidOperationException("synthetic classifier dependency failure"),
            _ => ValueTask.FromResult(ChatBotRiskClassification.Indeterminate),
        };
    }
}
