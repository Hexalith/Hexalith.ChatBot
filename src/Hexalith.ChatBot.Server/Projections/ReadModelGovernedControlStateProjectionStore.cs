using Hexalith.EventStore.Client.Projections;

namespace Hexalith.ChatBot.Server.Projections;

internal sealed class ReadModelGovernedControlStateProjectionStore(IReadModelStore store) : IGovernedControlStateProjectionStore
{
    public async Task<GovernedControlStateView?> GetAsync(
        string tenantId,
        string subjectClass,
        string subjectRef,
        CancellationToken cancellationToken = default)
        => (await store
            .GetAsync<GovernedControlStateView>(
                ChatBotReadModelStoreNames.StateStoreName,
                GovernedControlStateView.KeyFor(tenantId, subjectClass, subjectRef),
                cancellationToken)
            .ConfigureAwait(false)).Value;

    public async Task SaveAsync(GovernedControlStateView view, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(view);
        _ = await ReadModelWritePolicy
            .UpdateAsync<GovernedControlStateView>(
                store,
                ChatBotReadModelStoreNames.StateStoreName,
                GovernedControlStateView.KeyFor(view.TenantId, view.SubjectClass, view.SubjectRef),
                current => current is not null && current.SourceVersion > view.SourceVersion ? current : view,
                new ReadModelWriteContext(Category: nameof(GovernedControlStateView), ProjectionType: nameof(GovernedControlStateProjectionHandler), CorrelationId: view.CorrelationId),
                cancellationToken: cancellationToken)
            .ConfigureAwait(false);
        await UpsertIndexesAsync(view, cancellationToken).ConfigureAwait(false);
    }

    public async Task<IReadOnlyList<string>> EnumerateTenantIdsAsync(CancellationToken cancellationToken = default)
    {
        GovernedControlTenantIndex index = await GetTenantIndexAsync(cancellationToken).ConfigureAwait(false);
        return index.TenantIds;
    }

    public async Task<IReadOnlyList<GovernedControlStateView>> ReadRefreshCandidatesAsync(
        string tenantId,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(tenantId);

        GovernedControlSubjectIndex index = await GetSubjectIndexAsync(tenantId, cancellationToken).ConfigureAwait(false);
        List<GovernedControlStateView> views = [];
        foreach (string key in index.StateKeys)
        {
            GovernedControlStateView? view = (await store
                .GetAsync<GovernedControlStateView>(
                    ChatBotReadModelStoreNames.StateStoreName,
                    key,
                    cancellationToken)
                .ConfigureAwait(false)).Value;
            if (view is not null && string.Equals(view.TenantId, tenantId, StringComparison.Ordinal))
            {
                views.Add(view);
            }
        }

        return views
            .OrderBy(static view => view.SubjectClass, StringComparer.Ordinal)
            .ThenBy(static view => view.SubjectRef, StringComparer.Ordinal)
            .ToArray();
    }

    public async Task<bool> TryRefreshFreshnessAsync(
        GovernedControlStateView trustedView,
        DateTimeOffset refreshedAtUtc,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(trustedView);
        GovernedControlStateView? current = await GetAsync(
            trustedView.TenantId,
            trustedView.SubjectClass,
            trustedView.SubjectRef,
            cancellationToken)
            .ConfigureAwait(false);
        if (current is null || !IsSameTrustedState(current, trustedView))
        {
            return false;
        }

        _ = await ReadModelWritePolicy
            .UpdateAsync<GovernedControlStateView>(
                store,
                ChatBotReadModelStoreNames.StateStoreName,
                GovernedControlStateView.KeyFor(current.TenantId, current.SubjectClass, current.SubjectRef),
                // Freshness-only refresh under optimistic concurrency: if a concurrent control-state/rate-limit
                // event advanced the persisted record between the snapshot read above and this retry-safe read,
                // `latest` no longer matches the trusted snapshot. Yield to that newer value (write it back
                // unchanged) instead of overwriting it with the stale `current` snapshot — re-persisting `current`
                // would downgrade a higher-version record and could reactivate a disabled/quarantined subject.
                latest => latest is not null && IsSameTrustedState(latest, trustedView)
                    ? latest with { LastUpdatedAtUtc = refreshedAtUtc.ToUniversalTime() }
                    : latest ?? current,
                new ReadModelWriteContext(Category: nameof(GovernedControlStateView), ProjectionType: nameof(TryRefreshFreshnessAsync), CorrelationId: current.CorrelationId),
                cancellationToken: cancellationToken)
            .ConfigureAwait(false);
        return true;
    }

    private async Task UpsertIndexesAsync(GovernedControlStateView view, CancellationToken cancellationToken)
    {
        GovernedControlTenantIndex tenantIndex = await GetTenantIndexAsync(cancellationToken).ConfigureAwait(false);
        string[] tenantIds = tenantIndex.TenantIds
            .Concat([view.TenantId])
            .Distinct(StringComparer.Ordinal)
            .Order(StringComparer.Ordinal)
            .ToArray();
        _ = await ReadModelWritePolicy
            .UpdateAsync<GovernedControlTenantIndex>(
                store,
                ChatBotReadModelStoreNames.StateStoreName,
                TenantIndexKey(),
                _ => new GovernedControlTenantIndex(tenantIds),
                new ReadModelWriteContext(Category: nameof(GovernedControlTenantIndex), ProjectionType: nameof(GovernedControlStateProjectionHandler), CorrelationId: view.CorrelationId),
                cancellationToken: cancellationToken)
            .ConfigureAwait(false);

        string stateKey = GovernedControlStateView.KeyFor(view.TenantId, view.SubjectClass, view.SubjectRef);
        GovernedControlSubjectIndex subjectIndex = await GetSubjectIndexAsync(view.TenantId, cancellationToken).ConfigureAwait(false);
        string[] stateKeys = subjectIndex.StateKeys
            .Concat([stateKey])
            .Distinct(StringComparer.Ordinal)
            .Order(StringComparer.Ordinal)
            .ToArray();
        _ = await ReadModelWritePolicy
            .UpdateAsync<GovernedControlSubjectIndex>(
                store,
                ChatBotReadModelStoreNames.StateStoreName,
                SubjectIndexKeyFor(view.TenantId),
                _ => new GovernedControlSubjectIndex(view.TenantId, stateKeys),
                new ReadModelWriteContext(Category: nameof(GovernedControlSubjectIndex), ProjectionType: nameof(GovernedControlStateProjectionHandler), CorrelationId: view.CorrelationId),
                cancellationToken: cancellationToken)
            .ConfigureAwait(false);
    }

    private async Task<GovernedControlTenantIndex> GetTenantIndexAsync(CancellationToken cancellationToken)
        => (await store
            .GetAsync<GovernedControlTenantIndex>(
                ChatBotReadModelStoreNames.StateStoreName,
                TenantIndexKey(),
                cancellationToken)
            .ConfigureAwait(false)).Value
            ?? new GovernedControlTenantIndex([]);

    private async Task<GovernedControlSubjectIndex> GetSubjectIndexAsync(string tenantId, CancellationToken cancellationToken)
        => (await store
            .GetAsync<GovernedControlSubjectIndex>(
                ChatBotReadModelStoreNames.StateStoreName,
                SubjectIndexKeyFor(tenantId),
                cancellationToken)
            .ConfigureAwait(false)).Value
            ?? new GovernedControlSubjectIndex(tenantId, []);

    private static bool IsSameTrustedState(GovernedControlStateView current, GovernedControlStateView trusted)
        => current.SourceVersion == trusted.SourceVersion &&
            string.Equals(current.ControlState, trusted.ControlState, StringComparison.Ordinal) &&
            current.RateLimitBudget == trusted.RateLimitBudget &&
            string.Equals(current.RateLimitWindow, trusted.RateLimitWindow, StringComparison.Ordinal) &&
            current.RevocationSensitive == trusted.RevocationSensitive;

    private static string TenantIndexKey()
        => "governed-control:tenants:index";

    private static string SubjectIndexKeyFor(string tenantId)
        => $"{tenantId}:governed-control:subjects:index";

    private sealed record GovernedControlTenantIndex(IReadOnlyList<string> TenantIds);

    private sealed record GovernedControlSubjectIndex(string TenantId, IReadOnlyList<string> StateKeys);
}
