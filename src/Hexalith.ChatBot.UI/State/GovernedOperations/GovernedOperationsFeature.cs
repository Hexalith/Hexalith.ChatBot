using Fluxor;

namespace Hexalith.ChatBot.UI.State.GovernedOperations;

/// <summary>Fluxor feature registering the governed-operations slice and its initial (empty) state.</summary>
public sealed class GovernedOperationsFeature : Feature<GovernedOperationsState>
{
    /// <inheritdoc/>
    public override string GetName() => "GovernedOperations";

    /// <inheritdoc/>
    protected override GovernedOperationsState GetInitialState()
        => new(IsSubmitting: false, Outcome: null, Error: null);
}
