using Hexalith.ChatBot.Contracts.Queries;

namespace Hexalith.ChatBot.UI.State.OperationalDashboards;

/// <summary>Dispatched to load or refresh the read-only operational health overview within the staleness window.</summary>
public sealed record LoadOperationalDashboardAction;

/// <summary>Dispatched when the metadata-only health overview was read back.</summary>
/// <param name="Overview">The metadata-only operational dashboard overview.</param>
public sealed record OperationalDashboardLoadedAction(OperationalDashboardOverview Overview);

/// <summary>Dispatched when the overview load failed; carries a safe metadata-only error code only.</summary>
/// <param name="Error">The safe metadata-only error code.</param>
public sealed record OperationalDashboardLoadFailedAction(string Error);
