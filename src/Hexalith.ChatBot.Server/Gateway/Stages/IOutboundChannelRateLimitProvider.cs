using Hexalith.ChatBot.Contracts.Commands;
using Hexalith.ChatBot.Contracts.Enums;

namespace Hexalith.ChatBot.Server.Gateway.Stages;

/// <summary>
/// The per-(tenant × outbound-channel) send rate-limit budget observed at the outbound send seam in
/// <see cref="AcceptedCommandDispatcher"/>'s <c>ExecuteApprovedOutboundDraft</c> branch (Story 7.26). A
/// <see langword="null"/> provider result means the outbound channel has no configured limit. The budget is bounded by
/// <see cref="OutboundChannelRateLimitBounds"/>; an out-of-bounds budget falls back to the safe default at the
/// enforcement seam (never raises the cap). Mirrors the Story 7.23 <c>CommandCapabilityRateLimitState</c> shape,
/// scoped to a single outbound channel (the safe <c>AdapterRef</c> token) and measured over admitted sends — a
/// DEDICATED outbound-channel plane, kept independent from the per-actor / command-capability rate-limit planes
/// (subject-class separation, NFR30) and from the <c>IOutboundChannelControlStateProvider</c> control plane.
/// </summary>
/// <param name="Budget">The configured per-window send budget for the outbound channel.</param>
/// <param name="Window">The trailing rolling window the budget is measured over.</param>
internal sealed record OutboundChannelRateLimitState(int Budget, OutboundChannelRateLimitWindow Window)
{
    /// <summary>
    /// Gets the effective in-bounds budget: an out-of-bounds configured budget falls back to
    /// <see cref="OutboundChannelRateLimitBounds.SafeDefaults"/> — never silently raising the cap above the declared
    /// maximum.
    /// </summary>
    public int EffectiveBudget
        => new OutboundChannelRateLimitBounds(Budget).IsWithinBounds
            ? Budget
            : OutboundChannelRateLimitBounds.SafeDefaults.HourlySendBudget;

    /// <summary>Gets the trailing rolling window duration for <see cref="Window"/>.</summary>
    public TimeSpan WindowDuration
        => Window switch
        {
            OutboundChannelRateLimitWindow.RollingHour => TimeSpan.FromHours(1),
            _ => TimeSpan.FromHours(1),
        };
}

/// <summary>
/// Safe, finite capacity-impact observation emitted at the send seam when a rate-limit budget applies to an outbound
/// channel (Story 7.26, AC6). Carries integer-only tokens — the effective budget, the observed trailing-window
/// admitted-send count, and whether this send was throttled — mirroring the Story 7.23
/// <c>CommandCapabilityRateLimitObservation</c> shape. The throttled count is the backlog/degradation signal (sends
/// held back to keep external volume within tenant policy). This is the audit/observable seam only; full Epic-8
/// operational-dashboard wiring (and the runtime emission of this observation, deferred together with the read-side)
/// is out of scope.
/// </summary>
/// <param name="Budget">The effective in-bounds per-window budget for the outbound channel.</param>
/// <param name="ObservedWindowCount">The channel's admitted-send count in the trailing window at decision time.</param>
/// <param name="Throttled">Whether this send was throttled (rejected) because the budget was reached.</param>
internal sealed record OutboundChannelRateLimitObservation(int Budget, int ObservedWindowCount, bool Throttled);

/// <summary>
/// Read-side seam exposing the FR74/FR75 per-(tenant × outbound-channel) send rate-limit budget to the outbound send
/// seam. The durable projection of the <c>OutboundChannelRateLimitConfigured</c> event into this provider is deferred
/// (mirroring the Story 7.12/7.15/7.18/7.20/7.21/7.23/7.24/7.25 sanctioned read-side deferral): the default
/// implementation always reports no limit (<see langword="null"/>), and tests inject a fake reporting a configured
/// budget to exercise the dispatcher send-seam rate-limit gate in isolation. A dedicated outbound-channel seam —
/// never reusing the per-actor/command-capability history/provider or the control-state provider — keeps the FR74
/// subject classes independent (NFR30 isolation).
/// </summary>
internal interface IOutboundChannelRateLimitProvider
{
    /// <summary>
    /// Resolves the configured rate-limit budget for the given outbound channel (safe <c>AdapterRef</c> token) within
    /// the authenticated tenant. Returns <see langword="null"/> when no rate-limit has been configured/projected.
    /// </summary>
    ValueTask<OutboundChannelRateLimitState?> GetRateLimitAsync(
        string tenantId,
        string outboundChannelRef,
        CancellationToken cancellationToken);
}

/// <summary>
/// Default <see cref="IOutboundChannelRateLimitProvider"/> that always reports no configured limit. The durable
/// projection feeding a budget is deferred per the sanctioned read-side deferral; the enforcement seam is wired and
/// unit-tested with a fake.
/// </summary>
internal sealed class AlwaysUnlimitedOutboundChannelRateLimitProvider : IOutboundChannelRateLimitProvider
{
    public ValueTask<OutboundChannelRateLimitState?> GetRateLimitAsync(
        string tenantId,
        string outboundChannelRef,
        CancellationToken cancellationToken)
        => ValueTask.FromResult<OutboundChannelRateLimitState?>(null);
}

/// <summary>
/// Read-side seam exposing the per-(tenant × outbound-channel) recent admitted-send timestamps to the outbound send
/// seam (Story 7.26). The durable history (incremented only on a successful send) is deferred per the same read-side
/// deferral; the default implementation reports an empty history, and tests pre-seed N timestamps to simulate N
/// admitted sends in the trailing window. Each outbound channel's history is independent (NFR30 isolation) and kept
/// separate from the per-actor / command-capability histories (subject-class separation).
/// </summary>
internal interface IOutboundChannelSendHistory
{
    /// <summary>
    /// Resolves the recent admitted-send timestamps for the given outbound channel (safe <c>AdapterRef</c> token)
    /// within the authenticated tenant. Defaults to an empty history when none has been projected. The trailing-window
    /// count is measured server-side in UTC against the injected clock — never against client/item-supplied time.
    /// </summary>
    ValueTask<IReadOnlyList<DateTimeOffset>> GetRecentSendsAsync(
        string tenantId,
        string outboundChannelRef,
        CancellationToken cancellationToken);
}

/// <summary>
/// Default <see cref="IOutboundChannelSendHistory"/> that always reports an empty admitted-send history (so the seam
/// sends by default). The durable per-(tenant × outbound-channel) history is deferred per the sanctioned read-side
/// deferral.
/// </summary>
internal sealed class EmptyOutboundChannelSendHistory : IOutboundChannelSendHistory
{
    public ValueTask<IReadOnlyList<DateTimeOffset>> GetRecentSendsAsync(
        string tenantId,
        string outboundChannelRef,
        CancellationToken cancellationToken)
        => ValueTask.FromResult<IReadOnlyList<DateTimeOffset>>([]);
}
