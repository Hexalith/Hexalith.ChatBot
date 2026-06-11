using Hexalith.ChatBot.Contracts.Commands;
using Hexalith.ChatBot.Contracts.Enums;

namespace Hexalith.ChatBot.Server.Gateway.Stages;

/// <summary>
/// The per-(tenant × command-type) command rate-limit budget observed by the actor-agnostic admission seam
/// (Story 7.23). A <see langword="null"/> provider result means the command capability has no configured limit. The
/// budget is bounded by <see cref="CommandCapabilityRateLimitBounds"/>; an out-of-bounds budget falls back to the
/// safe default at the enforcement seam (never raises the cap). Mirrors the Story 7.20 <c>AiActorRateLimitState</c>
/// shape, scoped to a single command TYPE and measured over admitted commands — a DEDICATED command-capability plane,
/// kept independent from the per-actor rate-limit planes (subject-class separation, NFR30).
/// </summary>
/// <param name="Budget">The configured per-window command budget for the command type.</param>
/// <param name="Window">The trailing rolling window the budget is measured over.</param>
internal sealed record CommandCapabilityRateLimitState(int Budget, CommandCapabilityRateLimitWindow Window)
{
    /// <summary>
    /// Gets the effective in-bounds budget: an out-of-bounds configured budget falls back to
    /// <see cref="CommandCapabilityRateLimitBounds.SafeDefaults"/> — never silently raising the cap above the
    /// declared maximum.
    /// </summary>
    public int EffectiveBudget
        => new CommandCapabilityRateLimitBounds(Budget).IsWithinBounds
            ? Budget
            : CommandCapabilityRateLimitBounds.SafeDefaults.HourlyCommandBudget;

    /// <summary>Gets the trailing rolling window duration for <see cref="Window"/>.</summary>
    public TimeSpan WindowDuration
        => Window switch
        {
            CommandCapabilityRateLimitWindow.RollingHour => TimeSpan.FromHours(1),
            _ => TimeSpan.FromHours(1),
        };
}

/// <summary>
/// Safe, finite capacity-impact observation emitted at the admission seam when a rate-limit budget applies to a
/// command capability (Story 7.23, AC6). Carries integer-only tokens — the effective budget, the observed
/// trailing-window admitted-command count, and whether this submission was throttled — mirroring the Story 7.20
/// <c>AiActorRateLimitObservation</c> shape. The throttled count is the backlog/degradation signal (commands held
/// back to protect the tenant workflow). This is the audit/observable seam only; full Epic-8 operational-dashboard
/// wiring (and the runtime emission of this observation, deferred together with the read-side) is out of scope.
/// </summary>
/// <param name="Budget">The effective in-bounds per-window budget for the command type.</param>
/// <param name="ObservedWindowCount">The command type's admitted-command count in the trailing window at decision time.</param>
/// <param name="Throttled">Whether this submission was throttled (denied) because the budget was reached.</param>
internal sealed record CommandCapabilityRateLimitObservation(int Budget, int ObservedWindowCount, bool Throttled);

/// <summary>
/// Read-side seam exposing the FR74/FR75 per-(tenant × command-type) command rate-limit budget to the actor-agnostic
/// admission pipeline. The durable projection of the <c>CommandCapabilityRateLimitConfigured</c> event into this
/// provider is deferred (mirroring the Story 7.20–7.22 sanctioned read-side deferral): the default implementation
/// always reports no limit (<see langword="null"/>), and tests inject a fake reporting a configured budget to exercise
/// the <see cref="ParticipantAuthorizationStage"/> final-gate rate-limit branch in isolation. A dedicated
/// command-capability seam — never reusing the per-actor history/provider — keeps the FR74 subject classes
/// independent (NFR30 isolation).
/// </summary>
internal interface ICommandCapabilityRateLimitProvider
{
    /// <summary>
    /// Resolves the configured rate-limit budget for the given command capability (command type name) within the
    /// authenticated tenant. Returns <see langword="null"/> when no rate-limit has been configured/projected.
    /// </summary>
    ValueTask<CommandCapabilityRateLimitState?> GetRateLimitAsync(
        string tenantId,
        string commandCapabilityRef,
        CancellationToken cancellationToken);
}

/// <summary>
/// Default <see cref="ICommandCapabilityRateLimitProvider"/> that always reports no configured limit. The durable
/// projection feeding a budget is deferred per the Story 7.20–7.22 read-side deferral; the enforcement seam is wired
/// and unit-tested with a fake.
/// </summary>
internal sealed class AlwaysUnlimitedCommandCapabilityRateLimitProvider : ICommandCapabilityRateLimitProvider
{
    public ValueTask<CommandCapabilityRateLimitState?> GetRateLimitAsync(
        string tenantId,
        string commandCapabilityRef,
        CancellationToken cancellationToken)
        => ValueTask.FromResult<CommandCapabilityRateLimitState?>(null);
}

/// <summary>
/// Read-side seam exposing the per-(tenant × command-type) recent admitted-command timestamps to the actor-agnostic
/// admission pipeline (Story 7.23). The durable history (incremented only on a successful admission) is deferred per
/// the same read-side deferral; the default implementation reports an empty history, and tests pre-seed N timestamps
/// to simulate N admitted commands in the trailing window. Each command type's history is independent (NFR30
/// isolation) and kept separate from the per-actor command/proposal histories (subject-class separation).
/// </summary>
internal interface ICommandCapabilityCommandHistory
{
    /// <summary>
    /// Resolves the recent admitted-command timestamps for the given command capability (command type name) within
    /// the authenticated tenant. Defaults to an empty history when none has been projected. The trailing-window count
    /// is measured server-side in UTC against the injected clock — never against client/item-supplied time.
    /// </summary>
    ValueTask<IReadOnlyList<DateTimeOffset>> GetRecentAdmittedAsync(
        string tenantId,
        string commandCapabilityRef,
        CancellationToken cancellationToken);

    ValueTask RecordAdmittedAsync(
        string tenantId,
        string commandCapabilityRef,
        DateTimeOffset admittedAtUtc,
        CancellationToken cancellationToken)
        => ValueTask.CompletedTask;
}

/// <summary>
/// Default <see cref="ICommandCapabilityCommandHistory"/> that always reports an empty admitted-command history (so
/// the seam admits by default). The durable per-(tenant × command-type) history is deferred per the Story 7.20–7.22
/// read-side deferral.
/// </summary>
internal sealed class EmptyCommandCapabilityCommandHistory : ICommandCapabilityCommandHistory
{
    public ValueTask<IReadOnlyList<DateTimeOffset>> GetRecentAdmittedAsync(
        string tenantId,
        string commandCapabilityRef,
        CancellationToken cancellationToken)
        => ValueTask.FromResult<IReadOnlyList<DateTimeOffset>>([]);
}
