using Fluxor;

namespace Hexalith.ChatBot.UI.State.OperationalDashboards;

/// <summary>Fluxor feature registering the operational-dashboards slice and its initial (empty) state.</summary>
public sealed class OperationalDashboardsFeature : Feature<OperationalDashboardsState>
{
    /// <inheritdoc/>
    public override string GetName() => "OperationalDashboards";

    /// <inheritdoc/>
    protected override OperationalDashboardsState GetInitialState()
        => new(IsLoading: false, Overview: null, Error: null);
}
