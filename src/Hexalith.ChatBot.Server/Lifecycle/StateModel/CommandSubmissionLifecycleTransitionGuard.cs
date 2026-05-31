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
            _ => new LifecycleTransitionDefinition(LifecycleStates.Received, LifecycleStates.Proposed),
        };

        return LifecycleTransitionValidator.Validate(transition);
    }
}
