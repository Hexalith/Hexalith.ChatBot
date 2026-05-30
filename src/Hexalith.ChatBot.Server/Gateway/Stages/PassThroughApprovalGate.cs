using Hexalith.ChatBot.Server.Gateway;

namespace Hexalith.ChatBot.Server.Gateway.Stages;

internal sealed class PassThroughApprovalGate : IApprovalGate
{
    public ValueTask<ChatBotApprovalResult> EvaluateAsync(ChatBotGatewayContext context, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        return ValueTask.FromResult(ChatBotApprovalResult.Approved);
    }
}
