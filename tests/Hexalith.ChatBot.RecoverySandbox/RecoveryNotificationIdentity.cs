namespace Hexalith.ChatBot.RecoverySandbox;

/// <summary>Builds the closed provider-message identities used by recovery checkpoint and replay exercises.</summary>
internal static class RecoveryNotificationIdentity
{
    public const string HeaderName = "X-Recovery-Notification-Phase";
    public const string CheckpointPhase = "checkpoint";
    public const string RecoveryPhase = "recovery";

    /// <summary>Returns a stable identity per scenario lane and phase, rejecting open-ended controller input.</summary>
    public static string Compose(string providerMessageId, string scenarioLane, string phase)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(providerMessageId);
        if (scenarioLane is not ("continuity" or "graph") || phase is not (CheckpointPhase or RecoveryPhase))
        {
            throw new InvalidOperationException("The recovery notification lane or phase is outside the closed sandbox contract.");
        }

        return $"{providerMessageId}-{scenarioLane}-{phase}";
    }
}
