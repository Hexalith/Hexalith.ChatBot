using Hexalith.ChatBot.Server.Audit;

namespace Hexalith.ChatBot.Server.Projections;

internal sealed class GovernedControlStateProjectionHandler(
    IGovernedControlStateProjectionStore store,
    ISystemClock clock)
{
    public enum ProjectionOutcome
    {
        Applied,
        Ignored,
    }

    public async Task<ProjectionOutcome> HandleAsync(
        GovernedControlStateProjectionNotification notification,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(notification);

        GovernedControlStateView? existing = await store
            .GetAsync(notification.TenantId, notification.SubjectClass, notification.SubjectRef, cancellationToken)
            .ConfigureAwait(false);

        if (existing is not null && existing.SourceVersion >= notification.SourceVersion)
        {
            return ProjectionOutcome.Ignored;
        }

        bool controlStateUpdate = notification.Dimension == GovernedControlDimension.ControlState;

        // Control-state and rate-limit are orthogonal dimensions sharing one read-model record. Overlay only the
        // dimension this event changed and carry the other dimension forward from the existing record (or its safe
        // default when none exists yet): a rate-limit event must not re-activate a disabled/quarantined subject, and a
        // control-state event must not wipe a previously configured budget. Admitted history is always preserved.
        GovernedControlStateView view = new(
            notification.TenantId,
            notification.SubjectClass,
            notification.SubjectRef,
            controlStateUpdate ? notification.ControlState : existing?.ControlState ?? GovernedControlStateView.Active,
            controlStateUpdate ? existing?.RateLimitBudget : notification.RateLimitBudget,
            controlStateUpdate ? existing?.RateLimitWindow : notification.RateLimitWindow,
            notification.SourceVersion,
            notification.CorrelationId,
            notification.EffectiveAtUtc,
            clock.UtcNow,
            controlStateUpdate ? notification.RevocationSensitive : existing?.RevocationSensitive ?? notification.RevocationSensitive,
            existing?.RecentAdmittedAtUtc ?? []);

        await store.SaveAsync(view, cancellationToken).ConfigureAwait(false);
        return ProjectionOutcome.Applied;
    }
}
