using Hexalith.ChatBot.Server.Gateway;

namespace Hexalith.ChatBot.Server.Lifecycle.StateModel;

internal sealed class CommandSubmissionLifecycleTransitionGuard : ILifecycleTransitionGuard
{
    public LifecycleTransitionValidation ValidateCommandSubmission(ChatBotGatewayContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        return LifecycleTransitionValidator.Validate(
            new LifecycleTransitionDefinition(LifecycleStates.Received, LifecycleStates.Proposed));
    }
}
