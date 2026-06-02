using Hexalith.ChatBot.Contracts.Commands;
using Hexalith.ChatBot.Contracts.Enums;

namespace Hexalith.ChatBot.Server.Gateway.Stages;

/// <summary>
/// The per-AI-actor proposal rate-limit budget observed by the admission seam (Story 7.20). A
/// <see langword="null"/> provider result means the AI actor has no configured limit. The budget is bounded by
/// <see cref="AiActorRateLimitBounds"/>; an out-of-bounds budget falls back to the safe default at the enforcement
/// seam (never raises the cap). Mirrors the Story 7.17 <c>ServiceClientRateLimitState</c> shape, scoped to a single
/// AI actor and measured over admitted proposals — a DEDICATED AI-actor plane, kept independent from the
/// service-client rate-limit state (subject-class separation, NFR30).
/// </summary>
/// <param name="Budget">The configured per-window proposal budget for the AI actor.</param>
/// <param name="Window">The trailing rolling window the budget is measured over.</param>
internal sealed record AiActorRateLimitState(int Budget, AiActorRateLimitWindow Window)
{
    /// <summary>
    /// Gets the effective in-bounds budget: an out-of-bounds configured budget falls back to
    /// <see cref="AiActorRateLimitBounds.SafeDefaults"/> — never silently raising the cap above the declared maximum.
    /// </summary>
    public int EffectiveBudget
        => new AiActorRateLimitBounds(Budget).IsWithinBounds
            ? Budget
            : AiActorRateLimitBounds.SafeDefaults.HourlyProposalBudget;

    /// <summary>Gets the trailing rolling window duration for <see cref="Window"/>.</summary>
    public TimeSpan WindowDuration
        => Window switch
        {
            AiActorRateLimitWindow.RollingHour => TimeSpan.FromHours(1),
            _ => TimeSpan.FromHours(1),
        };
}

/// <summary>
/// Safe, finite capacity-impact observation emitted at the admission seam when a rate-limit budget applies to an
/// AI actor (Story 7.20, AC6). Carries integer-only tokens — the effective budget, the observed trailing-window
/// admitted-proposal count, and whether this proposal was throttled — mirroring the Story 7.17
/// <c>ServiceClientRateLimitObservation</c> shape. The throttled count is the approval-fatigue / backlog signal
/// (proposals held back from reviewers). This is the audit/observable seam only; full Epic-8 operational-dashboard
/// wiring (and the runtime emission of this observation, deferred together with the read-side) is out of scope.
/// </summary>
/// <param name="Budget">The effective in-bounds per-window budget for the AI actor.</param>
/// <param name="ObservedWindowCount">The AI actor's admitted-proposal count in the trailing window at decision time.</param>
/// <param name="Throttled">Whether this proposal was throttled (denied) because the budget was reached.</param>
internal sealed record AiActorRateLimitObservation(int Budget, int ObservedWindowCount, bool Throttled);

/// <summary>
/// Read-side seam exposing the FR74/FR75 per-AI-actor proposal rate-limit budget to the admission pipeline.
/// The durable projection of the <c>AiActorRateLimitConfigured</c> event into this provider is deferred
/// (mirroring the Story 7.14–7.19 sanctioned read-side deferral): the default implementation always reports
/// no limit (<see langword="null"/>), and tests inject a fake reporting a configured budget to exercise the
/// <see cref="ServiceClientGrantValidator"/> final-gate AI-actor rate-limit branch in isolation. A dedicated
/// AI-actor seam — never reusing the service-client history/provider — keeps the two FR74 subject classes
/// independent (NFR30 isolation).
/// </summary>
internal interface IAiActorRateLimitProvider
{
    /// <summary>
    /// Resolves the configured rate-limit budget for the given AI actor within the authenticated tenant.
    /// Returns <see langword="null"/> when no rate-limit has been configured/projected.
    /// </summary>
    ValueTask<AiActorRateLimitState?> GetRateLimitAsync(
        string tenantId,
        string aiActorId,
        CancellationToken cancellationToken);
}

/// <summary>
/// Default <see cref="IAiActorRateLimitProvider"/> that always reports no configured limit. The durable
/// projection feeding a budget is deferred per the Story 7.14–7.19 read-side deferral; the validator seam is
/// wired and unit-tested with a fake.
/// </summary>
internal sealed class AlwaysUnlimitedAiActorRateLimitProvider : IAiActorRateLimitProvider
{
    public ValueTask<AiActorRateLimitState?> GetRateLimitAsync(
        string tenantId,
        string aiActorId,
        CancellationToken cancellationToken)
        => ValueTask.FromResult<AiActorRateLimitState?>(null);
}

/// <summary>
/// Read-side seam exposing the per-(tenant × AI-actor) recent admitted-proposal timestamps to the admission
/// pipeline (Story 7.20). The durable history (incremented only on a successful admission) is deferred per the same
/// read-side deferral; the default implementation reports an empty history, and tests pre-seed N timestamps to
/// simulate N admitted proposals in the trailing window. Each AI actor's history is independent (NFR30 isolation) and
/// kept separate from the service-client command history (subject-class separation).
/// </summary>
internal interface IAiActorProposalHistory
{
    /// <summary>
    /// Resolves the recent admitted-proposal timestamps for the given AI actor within the authenticated tenant.
    /// Defaults to an empty history when none has been projected. The trailing-window count is measured server-side in
    /// UTC against the injected clock — never against client/item-supplied time.
    /// </summary>
    ValueTask<IReadOnlyList<DateTimeOffset>> GetRecentAdmittedAsync(
        string tenantId,
        string aiActorId,
        CancellationToken cancellationToken);
}

/// <summary>
/// Default <see cref="IAiActorProposalHistory"/> that always reports an empty admitted-proposal history (so the
/// seam admits by default). The durable per-AI-actor history is deferred per the Story 7.14–7.19 read-side deferral.
/// </summary>
internal sealed class EmptyAiActorProposalHistory : IAiActorProposalHistory
{
    public ValueTask<IReadOnlyList<DateTimeOffset>> GetRecentAdmittedAsync(
        string tenantId,
        string aiActorId,
        CancellationToken cancellationToken)
        => ValueTask.FromResult<IReadOnlyList<DateTimeOffset>>([]);
}
