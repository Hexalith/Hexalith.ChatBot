using Hexalith.ChatBot.Server.Gateway;

namespace Hexalith.ChatBot.Server.Lifecycle.StateModel;

internal interface ILifecycleTransitionGuard
{
    LifecycleTransitionValidation ValidateCommandSubmission(ChatBotGatewayContext context);

    /// <summary>
    /// Resolves the guard-validated <c>Received-&gt;Skipped</c> transition for a terminal skip trigger
    /// (duplicate-suppression or out-of-scope mailbox). The edge is validated against
    /// <see cref="LifecycleTransitionValidator"/> so the skip disposition is never produced by a magic string.
    /// </summary>
    LifecycleTransitionValidation ResolveSkipTransition(LifecycleSkipTrigger trigger);
}
