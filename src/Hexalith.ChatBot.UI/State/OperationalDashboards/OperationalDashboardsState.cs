using Hexalith.ChatBot.Contracts.Queries;

namespace Hexalith.ChatBot.UI.State.OperationalDashboards;

/// <summary>
/// Fluxor state for the read-only operational dashboards page: whether a health-overview load/refresh is in
/// flight, the last metadata-only overview, and a safe (metadata-only) error code when a load fails.
/// </summary>
/// <param name="IsLoading">Whether a load/refresh is currently in flight.</param>
/// <param name="Overview">The last loaded metadata-only overview, or <see langword="null"/>.</param>
/// <param name="Error">A safe metadata-only error code, or <see langword="null"/>.</param>
public sealed record OperationalDashboardsState(
    bool IsLoading,
    OperationalDashboardOverview? Overview,
    string? Error);
