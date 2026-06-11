namespace Hexalith.ChatBot.Server.Projections;

internal interface IGovernedControlStateProjectionStore
{
    Task<GovernedControlStateView?> GetAsync(
        string tenantId,
        string subjectClass,
        string subjectRef,
        CancellationToken cancellationToken = default);

    Task SaveAsync(GovernedControlStateView view, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<string>> EnumerateTenantIdsAsync(CancellationToken cancellationToken = default);

    Task<IReadOnlyList<GovernedControlStateView>> ReadRefreshCandidatesAsync(
        string tenantId,
        CancellationToken cancellationToken = default);

    Task<bool> TryRefreshFreshnessAsync(
        GovernedControlStateView trustedView,
        DateTimeOffset refreshedAtUtc,
        CancellationToken cancellationToken = default);
}
