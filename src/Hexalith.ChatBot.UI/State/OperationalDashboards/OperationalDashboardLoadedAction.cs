using Hexalith.ChatBot.Contracts.Queries;

namespace Hexalith.ChatBot.UI.State.OperationalDashboards;

/// <summary>Dispatched when the metadata-only health overview was read back.</summary>
/// <param name="Overview">The metadata-only operational dashboard overview.</param>
public sealed record OperationalDashboardLoadedAction(OperationalDashboardOverview Overview);
