using Hexalith.ChatBot.Server.Gateway;
using Hexalith.ChatBot.Contracts.Commands;

namespace Hexalith.ChatBot.Server.Lifecycle.StateModel;

internal sealed class CommandSubmissionLifecycleTransitionGuard : ILifecycleTransitionGuard
{
    public LifecycleTransitionValidation ValidateCommandSubmission(ChatBotGatewayContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        string? commandType = context.Submission.Request.CommandType;
        LifecycleTransitionDefinition transition = commandType switch
        {
            nameof(AssociateEmailToProject) => new(LifecycleStates.NeedsReview, LifecycleStates.Associated),
            nameof(RejectEmailProjectAssociation) => new(LifecycleStates.NeedsReview, LifecycleStates.Rejected),
            nameof(DeferEmailProjectAssociation) => new(LifecycleStates.NeedsReview, LifecycleStates.Deferred),
            nameof(MarkEmailAssociationNeedsReview) => new(LifecycleStates.NeedsReview, LifecycleStates.NeedsReview),
            nameof(CorrectEmailProjectAssociation) => new(LifecycleStates.Associated, LifecycleStates.Corrected),
            nameof(ApproveMailboxSourceDisable) => new(LifecycleStates.Active, LifecycleStates.Disabled),
            nameof(ApproveServiceClientDisable) => new(LifecycleStates.Active, LifecycleStates.Disabled),
            nameof(ApproveAiActorDisable) => new(LifecycleStates.Active, LifecycleStates.Disabled),
            nameof(ApproveCommandCapabilityDisable) => new(LifecycleStates.Active, LifecycleStates.Disabled),
            nameof(ApproveOutboundChannelDisable) => new(LifecycleStates.Active, LifecycleStates.Disabled),
            nameof(ApproveOutboundChannelQuarantine) => new(LifecycleStates.Active, LifecycleStates.Quarantined),
            nameof(ApproveCommandCapabilityQuarantine) => new(LifecycleStates.Active, LifecycleStates.Quarantined),
            nameof(ApproveAiActorQuarantine) => new(LifecycleStates.Active, LifecycleStates.Quarantined),
            nameof(ApproveMailboxSourceQuarantine) => new(LifecycleStates.Active, LifecycleStates.Quarantined),
            nameof(ApproveServiceClientQuarantine) => new(LifecycleStates.Active, LifecycleStates.Quarantined),
            _ => new LifecycleTransitionDefinition(LifecycleStates.Received, LifecycleStates.Proposed),
        };

        return LifecycleTransitionValidator.Validate(transition);
    }

    public LifecycleTransitionValidation ResolveSkipTransition(LifecycleSkipTrigger trigger)
    {
        // Both M1 skip triggers (duplicate-suppression, out-of-scope mailbox) are dispositions of a received
        // mailbox item, so both map to the same canonical terminal edge. The switch makes the mapping explicit
        // and exhaustive — an unmapped trigger fails closed as an invalid transition rather than defaulting.
        LifecycleTransitionDefinition transition = trigger switch
        {
            LifecycleSkipTrigger.DuplicateSuppression => new(LifecycleStates.Received, LifecycleStates.Skipped),
            LifecycleSkipTrigger.OutOfScopeMailbox => new(LifecycleStates.Received, LifecycleStates.Skipped),
            _ => new LifecycleTransitionDefinition(LifecycleStates.Received, trigger.ToString()),
        };

        return LifecycleTransitionValidator.Validate(transition);
    }
}
