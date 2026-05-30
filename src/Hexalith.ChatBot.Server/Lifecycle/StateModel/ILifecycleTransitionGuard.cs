using Hexalith.ChatBot.Server.Gateway;

namespace Hexalith.ChatBot.Server.Lifecycle.StateModel;

internal interface ILifecycleTransitionGuard
{
    LifecycleTransitionValidation ValidateCommandSubmission(ChatBotGatewayContext context);
}
