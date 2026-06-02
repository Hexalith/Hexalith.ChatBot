namespace Hexalith.ChatBot.Server.Notifications;

/// <summary>
/// The per-recipient throttle decision for a candidate notification delivery (Story 7.9, NFR46): deliver it as an
/// immediate push, or roll it up into the pending digest because a rolling-window ceiling is reached. A throttled
/// notification is never dropped — it always becomes a digest entry.
/// </summary>
internal enum NotificationThrottleDecision
{
    /// <summary>The recipient is under both rolling-window ceilings — deliver as an immediate push.</summary>
    Deliver,

    /// <summary>A rolling-window ceiling is reached — roll the overflow notification into the recipient's digest.</summary>
    ThrottleToDigest,
}
