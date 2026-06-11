using Hexalith.ChatBot.Contracts.Commands;
using Hexalith.ChatBot.Contracts.Enums;

namespace Hexalith.ChatBot.Server.Gateway.Stages;

/// <summary>
/// The per-service-client command rate-limit budget observed by the admission seam (Story 7.17). A
/// <see langword="null"/> provider result means the client has no configured limit. The budget is bounded by
/// <see cref="ServiceClientRateLimitBounds"/>; an out-of-bounds budget falls back to the safe default at the
/// enforcement seam (never raises the cap). Mirrors the Story 7.14 <c>MailboxRateLimitState</c> shape, scoped to a
/// single service client and measured over admitted commands.
/// </summary>
/// <param name="Budget">The configured per-window command budget for the service client.</param>
/// <param name="Window">The trailing rolling window the budget is measured over.</param>
internal sealed record ServiceClientRateLimitState(int Budget, ServiceClientRateLimitWindow Window)
{
    /// <summary>
    /// Gets the effective in-bounds budget: an out-of-bounds configured budget falls back to
    /// <see cref="ServiceClientRateLimitBounds.SafeDefaults"/> — never silently raising the cap above the declared maximum.
    /// </summary>
    public int EffectiveBudget
        => new ServiceClientRateLimitBounds(Budget).IsWithinBounds
            ? Budget
            : ServiceClientRateLimitBounds.SafeDefaults.HourlyCommandBudget;

    /// <summary>Gets the trailing rolling window duration for <see cref="Window"/>.</summary>
    public TimeSpan WindowDuration
        => Window switch
        {
            ServiceClientRateLimitWindow.RollingHour => TimeSpan.FromHours(1),
            _ => TimeSpan.FromHours(1),
        };
}

/// <summary>
/// Safe, finite capacity-impact observation emitted at the admission seam when a rate-limit budget applies to a
/// service client (Story 7.17, AC6). Carries integer-only tokens — the effective budget, the observed trailing-window
/// admitted-command count, and whether this command was throttled — mirroring the Story 7.14
/// <c>MailboxRateLimitObservation</c> shape. This is the audit/observable seam only; full Epic-8 operational-dashboard
/// wiring is out of scope.
/// </summary>
/// <param name="Budget">The effective in-bounds per-window budget for the service client.</param>
/// <param name="ObservedWindowCount">The client's admitted-command count in the trailing window at decision time.</param>
/// <param name="Throttled">Whether this command was throttled (denied) because the budget was reached.</param>
internal sealed record ServiceClientRateLimitObservation(int Budget, int ObservedWindowCount, bool Throttled);

/// <summary>
/// Read-side seam exposing the FR74/FR75 per-service-client command rate-limit budget to the admission pipeline.
/// The durable projection of the <c>ServiceClientRateLimitConfigured</c> event into this provider is deferred
/// (mirroring the Story 7.14/7.15/7.16 sanctioned read-side deferral): the default implementation always reports
/// no limit (<see langword="null"/>), and tests inject a fake reporting a configured budget to exercise the
/// <see cref="ServiceClientGrantValidator"/> final-gate rate-limit branch in isolation.
/// </summary>
internal interface IServiceClientRateLimitProvider
{
    /// <summary>
    /// Resolves the configured rate-limit budget for the given service client within the authenticated tenant.
    /// Returns <see langword="null"/> when no rate-limit has been configured/projected.
    /// </summary>
    ValueTask<ServiceClientRateLimitState?> GetRateLimitAsync(
        string tenantId,
        string serviceClientId,
        CancellationToken cancellationToken);
}

/// <summary>
/// Default <see cref="IServiceClientRateLimitProvider"/> that always reports no configured limit. The durable
/// projection feeding a budget is deferred per the Story 7.14/7.15/7.16 read-side deferral; the validator seam is
/// wired and unit-tested with a fake.
/// </summary>
internal sealed class AlwaysUnlimitedServiceClientRateLimitProvider : IServiceClientRateLimitProvider
{
    public ValueTask<ServiceClientRateLimitState?> GetRateLimitAsync(
        string tenantId,
        string serviceClientId,
        CancellationToken cancellationToken)
        => ValueTask.FromResult<ServiceClientRateLimitState?>(null);
}

/// <summary>
/// Read-side seam exposing the per-(tenant × service-client) recent admitted-command timestamps to the admission
/// pipeline (Story 7.17). The durable history (incremented only on a successful admission) is deferred per the same
/// read-side deferral; the default implementation reports an empty history, and tests pre-seed N timestamps to
/// simulate N admitted commands in the trailing window. Each client's history is independent (NFR30 isolation).
/// </summary>
internal interface IServiceClientCommandHistory
{
    /// <summary>
    /// Resolves the recent admitted-command timestamps for the given service client within the authenticated tenant.
    /// Defaults to an empty history when none has been projected. The trailing-window count is measured server-side in
    /// UTC against the injected clock — never against client/item-supplied time.
    /// </summary>
    ValueTask<IReadOnlyList<DateTimeOffset>> GetRecentAdmittedAsync(
        string tenantId,
        string serviceClientId,
        CancellationToken cancellationToken);

    ValueTask RecordAdmittedAsync(
        string tenantId,
        string serviceClientId,
        DateTimeOffset admittedAtUtc,
        CancellationToken cancellationToken)
        => ValueTask.CompletedTask;
}

/// <summary>
/// Default <see cref="IServiceClientCommandHistory"/> that always reports an empty admitted-command history (so the
/// seam admits by default). The durable per-client history is deferred per the Story 7.14/7.15/7.16 read-side deferral.
/// </summary>
internal sealed class EmptyServiceClientCommandHistory : IServiceClientCommandHistory
{
    public ValueTask<IReadOnlyList<DateTimeOffset>> GetRecentAdmittedAsync(
        string tenantId,
        string serviceClientId,
        CancellationToken cancellationToken)
        => ValueTask.FromResult<IReadOnlyList<DateTimeOffset>>([]);
}
