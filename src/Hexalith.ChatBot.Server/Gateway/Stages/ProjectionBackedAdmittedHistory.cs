using Hexalith.ChatBot.Contracts.Enums;
using Hexalith.ChatBot.Server.Audit;
using Hexalith.ChatBot.Server.Projections;

namespace Hexalith.ChatBot.Server.Gateway.Stages;

internal abstract class ProjectionBackedAdmittedHistory(
    IGovernedControlStateProjectionStore store,
    ISystemClock clock,
    string subjectClass)
{
    protected async ValueTask<IReadOnlyList<DateTimeOffset>> ReadAsync(string tenantId, string subjectRef, CancellationToken cancellationToken)
    {
        GovernedControlStateView? view = await store.GetAsync(tenantId, subjectClass, subjectRef, cancellationToken).ConfigureAwait(false);
        return GovernedProjectionProviderHelpers.IsStale(view, clock.UtcNow) ? [clock.UtcNow] : view?.RecentAdmittedAtUtc ?? [];
    }

    protected async ValueTask RecordAsync(string tenantId, string subjectRef, DateTimeOffset admittedAtUtc, CancellationToken cancellationToken)
    {
        GovernedControlStateView? view = await store.GetAsync(tenantId, subjectClass, subjectRef, cancellationToken).ConfigureAwait(false);
        if (view is null)
        {
            return;
        }

        DateTimeOffset cutoff = admittedAtUtc.ToUniversalTime().AddHours(-1);
        DateTimeOffset[] updated = view.RecentAdmittedAtUtc
            .Where(timestamp => timestamp >= cutoff)
            .Append(admittedAtUtc.ToUniversalTime())
            .ToArray();
        await store.SaveAsync(view with { AdmittedAtUtc = updated, LastUpdatedAtUtc = clock.UtcNow }, cancellationToken).ConfigureAwait(false);
    }
}
