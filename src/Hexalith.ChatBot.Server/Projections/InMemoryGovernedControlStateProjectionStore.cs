using System.Collections.Concurrent;

namespace Hexalith.ChatBot.Server.Projections;

internal sealed class InMemoryGovernedControlStateProjectionStore : IGovernedControlStateProjectionStore
{
    private readonly ConcurrentDictionary<string, GovernedControlStateView> _views = new(StringComparer.Ordinal);

    public Task<GovernedControlStateView?> GetAsync(
        string tenantId,
        string subjectClass,
        string subjectRef,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        _ = _views.TryGetValue(GovernedControlStateView.KeyFor(tenantId, subjectClass, subjectRef), out GovernedControlStateView? view);
        return Task.FromResult(view);
    }

    public Task SaveAsync(GovernedControlStateView view, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(view);
        cancellationToken.ThrowIfCancellationRequested();
        _views[GovernedControlStateView.KeyFor(view.TenantId, view.SubjectClass, view.SubjectRef)] = view;
        return Task.CompletedTask;
    }

    public Task<IReadOnlyList<string>> EnumerateTenantIdsAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        string[] tenants = _views.Values
            .Select(static view => view.TenantId)
            .Distinct(StringComparer.Ordinal)
            .Order(StringComparer.Ordinal)
            .ToArray();
        return Task.FromResult<IReadOnlyList<string>>(tenants);
    }

    public Task<IReadOnlyList<GovernedControlStateView>> ReadRefreshCandidatesAsync(
        string tenantId,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(tenantId);
        cancellationToken.ThrowIfCancellationRequested();
        GovernedControlStateView[] views = _views.Values
            .Where(view => string.Equals(view.TenantId, tenantId, StringComparison.Ordinal))
            .OrderBy(static view => view.SubjectClass, StringComparer.Ordinal)
            .ThenBy(static view => view.SubjectRef, StringComparer.Ordinal)
            .ToArray();
        return Task.FromResult<IReadOnlyList<GovernedControlStateView>>(views);
    }

    public Task<bool> TryRefreshFreshnessAsync(
        GovernedControlStateView trustedView,
        DateTimeOffset refreshedAtUtc,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(trustedView);
        cancellationToken.ThrowIfCancellationRequested();
        string key = GovernedControlStateView.KeyFor(trustedView.TenantId, trustedView.SubjectClass, trustedView.SubjectRef);
        if (!_views.TryGetValue(key, out GovernedControlStateView? current) || !IsSameTrustedState(current, trustedView))
        {
            return Task.FromResult(false);
        }

        _views[key] = current with { LastUpdatedAtUtc = refreshedAtUtc.ToUniversalTime() };
        return Task.FromResult(true);
    }

    private static bool IsSameTrustedState(GovernedControlStateView current, GovernedControlStateView trusted)
        => current.SourceVersion == trusted.SourceVersion &&
            string.Equals(current.ControlState, trusted.ControlState, StringComparison.Ordinal) &&
            current.RateLimitBudget == trusted.RateLimitBudget &&
            string.Equals(current.RateLimitWindow, trusted.RateLimitWindow, StringComparison.Ordinal) &&
            current.RevocationSensitive == trusted.RevocationSensitive;
}
