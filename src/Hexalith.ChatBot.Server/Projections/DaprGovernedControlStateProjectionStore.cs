using Dapr.Client;

namespace Hexalith.ChatBot.Server.Projections;

internal sealed class DaprGovernedControlStateProjectionStore(DaprClient daprClient) : IGovernedControlStateProjectionStore
{
    public async Task<GovernedControlStateView?> GetAsync(
        string tenantId,
        string subjectClass,
        string subjectRef,
        CancellationToken cancellationToken = default)
        => await daprClient
            .GetStateAsync<GovernedControlStateView?>(
                DaprGovernedOperationViewStore.StateStoreName,
                GovernedControlStateView.KeyFor(tenantId, subjectClass, subjectRef),
                cancellationToken: cancellationToken)
            .ConfigureAwait(false);

    public async Task SaveAsync(GovernedControlStateView view, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(view);
        await daprClient
            .SaveStateAsync(
                DaprGovernedOperationViewStore.StateStoreName,
                GovernedControlStateView.KeyFor(view.TenantId, view.SubjectClass, view.SubjectRef),
                view,
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
            GovernedControlStateView? view = await daprClient
                .GetStateAsync<GovernedControlStateView?>(
                    DaprGovernedOperationViewStore.StateStoreName,
                    key,
                    cancellationToken: cancellationToken)
                .ConfigureAwait(false);
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

        await daprClient
            .SaveStateAsync(
                DaprGovernedOperationViewStore.StateStoreName,
                GovernedControlStateView.KeyFor(current.TenantId, current.SubjectClass, current.SubjectRef),
                current with { LastUpdatedAtUtc = refreshedAtUtc.ToUniversalTime() },
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
        await daprClient
            .SaveStateAsync(
                DaprGovernedOperationViewStore.StateStoreName,
                TenantIndexKey(),
                new GovernedControlTenantIndex(tenantIds),
                cancellationToken: cancellationToken)
            .ConfigureAwait(false);

        string stateKey = GovernedControlStateView.KeyFor(view.TenantId, view.SubjectClass, view.SubjectRef);
        GovernedControlSubjectIndex subjectIndex = await GetSubjectIndexAsync(view.TenantId, cancellationToken).ConfigureAwait(false);
        string[] stateKeys = subjectIndex.StateKeys
            .Concat([stateKey])
            .Distinct(StringComparer.Ordinal)
            .Order(StringComparer.Ordinal)
            .ToArray();
        await daprClient
            .SaveStateAsync(
                DaprGovernedOperationViewStore.StateStoreName,
                SubjectIndexKeyFor(view.TenantId),
                new GovernedControlSubjectIndex(view.TenantId, stateKeys),
                cancellationToken: cancellationToken)
            .ConfigureAwait(false);
    }

    private async Task<GovernedControlTenantIndex> GetTenantIndexAsync(CancellationToken cancellationToken)
        => await daprClient
            .GetStateAsync<GovernedControlTenantIndex?>(
                DaprGovernedOperationViewStore.StateStoreName,
                TenantIndexKey(),
                cancellationToken: cancellationToken)
            .ConfigureAwait(false)
            ?? new GovernedControlTenantIndex([]);

    private async Task<GovernedControlSubjectIndex> GetSubjectIndexAsync(string tenantId, CancellationToken cancellationToken)
        => await daprClient
            .GetStateAsync<GovernedControlSubjectIndex?>(
                DaprGovernedOperationViewStore.StateStoreName,
                SubjectIndexKeyFor(tenantId),
                cancellationToken: cancellationToken)
            .ConfigureAwait(false)
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
