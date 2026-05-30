using Hexalith.ChatBot.Server.Gateway;

namespace Hexalith.ChatBot.Server.Gateway.Stages;

internal interface IApprovalGate
{
    ValueTask<ChatBotApprovalResult> EvaluateAsync(ChatBotGatewayContext context, CancellationToken cancellationToken);
}
