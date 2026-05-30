namespace Hexalith.ChatBot.Server.Lifecycle.StateModel;

internal sealed record LifecycleTransitionValidation(
    bool IsValid,
    LifecycleTransitionDefinition Transition,
    string ReasonCode)
{
    public static LifecycleTransitionValidation Valid(LifecycleTransitionDefinition transition)
        => new(true, transition, LifecycleTransitionReasonCodes.ValidTransition);

    public static LifecycleTransitionValidation Invalid(LifecycleTransitionDefinition transition)
        => new(false, transition, LifecycleTransitionReasonCodes.InvalidTransition);
}
