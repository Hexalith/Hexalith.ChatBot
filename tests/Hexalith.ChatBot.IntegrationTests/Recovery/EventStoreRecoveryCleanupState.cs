namespace Hexalith.ChatBot.IntegrationTests.Recovery;

/// <summary>Tracks one EventStore recovery scenario generation until cleanup detaches it.</summary>
internal sealed class EventStoreRecoveryCleanupState
{
    /// <summary>Gets the governed-note references recorded before their durable confirmation waits.</summary>
    internal List<string> CheckpointNoteRefs { get; } = [];

    /// <summary>Gets the durable checkpoint timestamps confirmed for the tracked notes.</summary>
    internal List<DateTimeOffset> CheckpointCommittedAtUtc { get; } = [];

    /// <summary>Gets or sets the correlation identity shared by the tracked checkpoint notes.</summary>
    internal string? CheckpointCorrelationId { get; set; }

    /// <summary>Gets or sets the note used to prove a fault did not commit an unauthorized mutation.</summary>
    internal string? FaultProbeNoteRef { get; set; }

    /// <summary>Gets or sets the independent control-tenant note used by the isolation probe.</summary>
    internal string? ControlTenantNoteRef { get; set; }

    /// <summary>Gets whether this generation owns any state that cleanup must handle.</summary>
    internal bool HasOwnedState => CheckpointNoteRefs.Count > 0 ||
        !string.IsNullOrWhiteSpace(ControlTenantNoteRef) ||
        !string.IsNullOrWhiteSpace(FaultProbeNoteRef);

    /// <summary>Detaches the active generation and replaces it with a fresh empty generation.</summary>
    /// <param name="activeState">The active generation field to replace.</param>
    /// <returns>The detached generation that cleanup must use exclusively.</returns>
    internal static EventStoreRecoveryCleanupState DetachAndReset(
        ref EventStoreRecoveryCleanupState activeState)
    {
        ArgumentNullException.ThrowIfNull(activeState);
        EventStoreRecoveryCleanupState detached = activeState;
        activeState = new EventStoreRecoveryCleanupState();
        return detached;
    }
}
