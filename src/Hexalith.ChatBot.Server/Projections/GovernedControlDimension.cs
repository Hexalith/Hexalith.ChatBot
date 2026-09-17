namespace Hexalith.ChatBot.Server.Projections;

/// <summary>
/// Identifies which orthogonal governed-control dimension a projection notification updates. Control-state and
/// rate-limit events for the same subject share one read-model record (keyed by tenant × subject class × subject ref),
/// so the handler must overlay only the dimension a given event actually changed: a rate-limit event must never
/// re-activate a disabled/quarantined subject, and a control-state event must never wipe a previously configured budget.
/// </summary>
internal enum GovernedControlDimension
{
    ControlState,
    RateLimit,
}
