namespace Hexalith.ChatBot.Server.Lifecycle.StateModel;

internal static class LifecycleTransitionValidator
{
    private static readonly HashSet<LifecycleTransitionDefinition> ValidTransitions =
    [
        new(LifecycleStates.Received, LifecycleStates.Proposed),
        new(LifecycleStates.Received, LifecycleStates.NeedsReview),
        new(LifecycleStates.Received, LifecycleStates.Failed),
        new(LifecycleStates.Received, LifecycleStates.Skipped),
        new(LifecycleStates.Proposed, LifecycleStates.Associated),
        new(LifecycleStates.Proposed, LifecycleStates.Rejected),
        new(LifecycleStates.Proposed, LifecycleStates.Deferred),
        new(LifecycleStates.Proposed, LifecycleStates.NeedsReview),
        new(LifecycleStates.Proposed, LifecycleStates.Failed),
        new(LifecycleStates.Deferred, LifecycleStates.Proposed),
        new(LifecycleStates.Deferred, LifecycleStates.Rejected),
        new(LifecycleStates.Deferred, LifecycleStates.NeedsReview),
        new(LifecycleStates.NeedsReview, LifecycleStates.Proposed),
        new(LifecycleStates.NeedsReview, LifecycleStates.Associated),
        new(LifecycleStates.NeedsReview, LifecycleStates.Rejected),
        new(LifecycleStates.NeedsReview, LifecycleStates.Deferred),
        new(LifecycleStates.NeedsReview, LifecycleStates.NeedsReview),
        new(LifecycleStates.Associated, LifecycleStates.Corrected),
        new(LifecycleStates.Corrected, LifecycleStates.Correcting),
        new(LifecycleStates.Correcting, LifecycleStates.Corrected),
        new(LifecycleStates.Correcting, LifecycleStates.CorrectionDelayed),
        new(LifecycleStates.CorrectionDelayed, LifecycleStates.Corrected),
        new(LifecycleStates.Active, LifecycleStates.Disabled),
    ];

    public static LifecycleTransitionValidation Validate(LifecycleTransitionDefinition transition)
    {
        ArgumentNullException.ThrowIfNull(transition);

        return ValidTransitions.Contains(transition)
            ? LifecycleTransitionValidation.Valid(transition)
            : LifecycleTransitionValidation.Invalid(transition);
    }
}
